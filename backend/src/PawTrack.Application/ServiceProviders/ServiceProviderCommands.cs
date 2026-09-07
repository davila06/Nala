using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record PublicServiceProviderDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    string? Website,
    string? LogoUrl,
    bool IsFeatured,
    string Status,
    bool IsVerified = false)
{
    public static PublicServiceProviderDto FromDomain(ServiceProvider provider, bool isVerified = false) => new(
        provider.Id, provider.Name, provider.Description, provider.Category.ToString(),
        provider.Address, provider.Lat, provider.Lng, provider.PhoneNumber,
        provider.Website, provider.LogoUrl, provider.IsFeatured, provider.Status.ToString(), isVerified);
}

public sealed record RegisterServiceProviderCommand(
    string Name,
    string Description,
    ServiceProviderCategory Category,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Password) : IRequest<Result<PublicServiceProviderDto>>;

public sealed class RegisterServiceProviderCommandValidator : AbstractValidator<RegisterServiceProviderCommand>
{
    public RegisterServiceProviderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
    }
}

public sealed class RegisterServiceProviderCommandHandler(
    IUserRepository userRepository,
    IServiceProviderRepository providerRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    ILogger<RegisterServiceProviderCommandHandler> logger)
    : IRequestHandler<RegisterServiceProviderCommand, Result<PublicServiceProviderDto>>
{
    public const string DuplicateEmailError = "duplicate_email";

    public async Task<Result<PublicServiceProviderDto>> Handle(RegisterServiceProviderCommand request, CancellationToken ct)
    {
        if (await userRepository.GetByEmailAsync(request.ContactEmail, ct) is not null)
            return Result.Failure<PublicServiceProviderDto>(DuplicateEmailError);

        var hash = passwordHasher.Hash(request.Password);
        var (user, rawToken) = User.Create(request.ContactEmail, request.Name, hash);
        user.AssignServiceProviderRole();
        await userRepository.AddAsync(user, ct);

        var provider = ServiceProvider.Create(
            user.Id, request.Name, request.Description, request.Category, request.Address,
            request.Lat, request.Lng, request.ContactEmail);
        await providerRepository.AddAsync(provider, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _ = emailSender.SendEmailVerificationAsync(user.Email, user.Name, rawToken, ct)
            .ContinueWith(task => logger.LogWarning(task.Exception,
                "Service provider registration email failed for {Email}", PiiHelper.MaskEmail(user.Email)),
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);

        return Result.Success(PublicServiceProviderDto.FromDomain(provider));
    }
}

public sealed record GetPublicServiceProvidersQuery(
    ServiceProviderCategory? Category = null,
    ServiceModality? Modality = null,
    decimal? MinPriceCrc = null,
    decimal? MaxPriceCrc = null,
    int Page = 1,
    int PageSize = 50) : IRequest<Result<IReadOnlyList<PublicServiceProviderDto>>>;

public sealed class GetPublicServiceProvidersQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetPublicServiceProvidersQuery, Result<IReadOnlyList<PublicServiceProviderDto>>>
{
    public async Task<Result<IReadOnlyList<PublicServiceProviderDto>>> Handle(
        GetPublicServiceProvidersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        if (request.MinPriceCrc < 0 || request.MaxPriceCrc < 0 ||
            request.MinPriceCrc > request.MaxPriceCrc)
            return Result.Failure<IReadOnlyList<PublicServiceProviderDto>>("El rango de precio es invalido.");
        var providers = await repository.GetActivePagedAsync(
            request.Category, request.Modality, request.MinPriceCrc, request.MaxPriceCrc,
            (page - 1) * pageSize, pageSize, ct);
        var verifiedProviderIds = await repository.GetActiveVerifiedProviderIdsAsync(
            providers.Select(provider => provider.Id), ct);
        return Result.Success<IReadOnlyList<PublicServiceProviderDto>>(
            providers.Select(provider => PublicServiceProviderDto.FromDomain(provider, verifiedProviderIds.Contains(provider.Id))).ToList());
    }
}

public sealed record GetServiceProviderDetailQuery(Guid ServiceProviderId)
    : IRequest<Result<PublicServiceProviderDto>>;

public sealed class GetServiceProviderDetailQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetServiceProviderDetailQuery, Result<PublicServiceProviderDto>>
{
    public async Task<Result<PublicServiceProviderDto>> Handle(GetServiceProviderDetailQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByIdAsync(request.ServiceProviderId, ct);
        if (provider is null || provider.Status != ServiceProviderStatus.Active)
            return Result.Failure<PublicServiceProviderDto>("Proveedor no encontrado.");
        var verifiedProviderIds = await repository.GetActiveVerifiedProviderIdsAsync([provider.Id], ct);
        return Result.Success(PublicServiceProviderDto.FromDomain(provider, verifiedProviderIds.Contains(provider.Id)));
    }
}

public sealed record GetMyServiceProviderQuery(Guid UserId) : IRequest<Result<PublicServiceProviderDto>>;

public sealed class GetMyServiceProviderQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetMyServiceProviderQuery, Result<PublicServiceProviderDto>>
{
    public async Task<Result<PublicServiceProviderDto>> Handle(GetMyServiceProviderQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.UserId, ct);
        return provider is null
            ? Result.Failure<PublicServiceProviderDto>("Proveedor no encontrado.")
            : Result.Success(PublicServiceProviderDto.FromDomain(provider));
    }
}

public sealed record UpdateServiceProviderProfileCommand(
    Guid OwnerUserId,
    string Name,
    string Description,
    ServiceProviderCategory Category,
    string Address,
    decimal Lat,
    decimal Lng,
    string? PhoneNumber,
    string? Website) : IRequest<Result<PublicServiceProviderDto>>;

public sealed class UpdateServiceProviderProfileCommandValidator : AbstractValidator<UpdateServiceProviderProfileCommand>
{
    public UpdateServiceProviderProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateServiceProviderProfileCommandHandler(
    IServiceProviderRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateServiceProviderProfileCommand, Result<PublicServiceProviderDto>>
{
    public async Task<Result<PublicServiceProviderDto>> Handle(UpdateServiceProviderProfileCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<PublicServiceProviderDto>("Proveedor no encontrado.");

        provider.UpdateProfile(
            request.Name, request.Description, request.Category, request.Address,
            request.Lat, request.Lng, request.PhoneNumber, request.Website);
        repository.Update(provider);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(PublicServiceProviderDto.FromDomain(provider));
    }
}

public sealed record ReviewServiceProviderCommand(Guid AdminUserId, Guid ServiceProviderId, bool Approve)
    : IRequest<Result<Unit>>;

public sealed class ReviewServiceProviderCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    INotificationRepository? notificationRepository = null)
    : IRequestHandler<ReviewServiceProviderCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(ReviewServiceProviderCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByIdAsync(request.ServiceProviderId, ct);
        if (provider is null) return Result.Failure<Unit>("Proveedor no encontrado.");

        if (request.Approve) provider.Activate(); else provider.Reject();
        repository.Update(provider);
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.AdminUserId,
            request.Approve ? AuditAction.ServiceProviderApproved : AuditAction.ServiceProviderRejected,
            "ServiceProvider",
            provider.Id.ToString()), ct);
        if (notificationRepository is not null)
        {
            await notificationRepository.AddAsync(Notification.Create(
                provider.UserId,
                NotificationType.ProviderStatusUpdate,
                request.Approve ? "Perfil de proveedor aprobado" : "Perfil de proveedor rechazado",
                request.Approve
                    ? "Tu perfil fue aprobado y ya puede aparecer en el directorio."
                    : "Tu perfil no fue aprobado. Revisa los datos y vuelve a enviar la solicitud.",
                provider.Id.ToString()), ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

public sealed record SetServiceProviderOperationalStatusCommand(
    Guid AdminUserId,
    Guid ServiceProviderId,
    bool Suspend,
    string? Reason) : IRequest<Result<Unit>>;

public sealed class SetServiceProviderOperationalStatusCommandValidator : AbstractValidator<SetServiceProviderOperationalStatusCommand>
{
    public SetServiceProviderOperationalStatusCommandValidator()
    {
        RuleFor(command => command.Reason).NotEmpty().When(command => command.Suspend)
            .WithMessage("El motivo de suspension es requerido.");
    }
}

public sealed class SetServiceProviderOperationalStatusCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    INotificationRepository? notificationRepository = null)
    : IRequestHandler<SetServiceProviderOperationalStatusCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SetServiceProviderOperationalStatusCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByIdAsync(request.ServiceProviderId, ct);
        if (provider is null) return Result.Failure<Unit>("Proveedor no encontrado.");
        try
        {
            if (request.Suspend) provider.Suspend(request.Reason ?? string.Empty); else provider.Activate();
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<Unit>(exception.Message);
        }

        repository.Update(provider);
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.AdminUserId,
            request.Suspend ? AuditAction.ServiceProviderSuspended : AuditAction.ServiceProviderApproved,
            "ServiceProvider",
            provider.Id.ToString(),
            request.Suspend ? request.Reason : "Reactivado"), ct);
        if (notificationRepository is not null)
        {
            await notificationRepository.AddAsync(Notification.Create(
                provider.UserId,
                NotificationType.ProviderStatusUpdate,
                request.Suspend ? "Perfil de proveedor suspendido" : "Perfil de proveedor reactivado",
                request.Suspend
                    ? "Tu perfil fue suspendido temporalmente. Contacta a soporte para mas informacion."
                    : "Tu perfil fue reactivado y puede volver a aparecer en el directorio.",
                provider.Id.ToString()), ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

public sealed record ServiceProviderAdminDto(
    Guid Id,
    string Name,
    string Category,
    string Address,
    string Status,
    string? SuspensionReason,
    DateTimeOffset RegisteredAt)
{
    public static ServiceProviderAdminDto FromDomain(ServiceProvider provider) => new(
        provider.Id, provider.Name, provider.Category.ToString(), provider.Address,
        provider.Status.ToString(), provider.SuspensionReason, provider.RegisteredAt);
}

public sealed record GetServiceProvidersForAdminQuery(int Page = 1, int PageSize = 50)
    : IRequest<Result<IReadOnlyList<ServiceProviderAdminDto>>>;

public sealed class GetServiceProvidersForAdminQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetServiceProvidersForAdminQuery, Result<IReadOnlyList<ServiceProviderAdminDto>>>
{
    public async Task<Result<IReadOnlyList<ServiceProviderAdminDto>>> Handle(GetServiceProvidersForAdminQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.PageSize, 1, 100);
        var skip = (Math.Max(1, request.Page) - 1) * take;
        var providers = await repository.GetOperationalProvidersAsync(skip, take, ct);
        return Result.Success<IReadOnlyList<ServiceProviderAdminDto>>(providers.Select(ServiceProviderAdminDto.FromDomain).ToList());
    }
}

public sealed record ProviderServiceDto(
    Guid Id,
    Guid ServiceProviderId,
    string Name,
    string Description,
    string Modality,
    int DurationMinutes,
    decimal PriceCrc,
    int Capacity,
    string Status)
{
    public static ProviderServiceDto FromDomain(ProviderService service) => new(
        service.Id, service.ServiceProviderId, service.Name, service.Description,
        service.Modality.ToString(), service.DurationMinutes, service.PriceCrc,
        service.Capacity, service.Status.ToString());
}

public sealed record AddProviderServiceCommand(
    Guid OwnerUserId,
    string Name,
    string Description,
    ServiceModality Modality,
    int DurationMinutes,
    decimal PriceCrc,
    int Capacity) : IRequest<Result<ProviderServiceDto>>;

public sealed class AddProviderServiceCommandValidator : AbstractValidator<AddProviderServiceCommand>
{
    public AddProviderServiceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 1_440);
        RuleFor(x => x.PriceCrc).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 100);
    }
}

public sealed class AddProviderServiceCommandHandler(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<AddProviderServiceCommand, Result<ProviderServiceDto>>
{
    public async Task<Result<ProviderServiceDto>> Handle(AddProviderServiceCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<ProviderServiceDto>("Proveedor no encontrado.");
        if (provider.Status != ServiceProviderStatus.Active)
            return Result.Failure<ProviderServiceDto>("El proveedor debe estar activo para publicar servicios.");

        var service = ProviderService.Create(
            provider.Id, request.Name, request.Description, request.Modality,
            request.DurationMinutes, request.PriceCrc, request.Capacity);
        await repository.AddServiceAsync(service, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderServiceDto.FromDomain(service));
    }
}

public sealed record SetProviderServiceStatusCommand(
    Guid OwnerUserId,
    Guid ProviderServiceId,
    ProviderServiceStatus Status) : IRequest<Result<ProviderServiceDto>>;

public sealed class SetProviderServiceStatusCommandHandler(
    IServiceProviderRepository repository,
    IUnitOfWork unitOfWork,
    IAuditLogRepository? auditLog = null)
    : IRequestHandler<SetProviderServiceStatusCommand, Result<ProviderServiceDto>>
{
    public async Task<Result<ProviderServiceDto>> Handle(SetProviderServiceStatusCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (provider is null || service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<ProviderServiceDto>("Servicio no encontrado.");

        switch (request.Status)
        {
            case ProviderServiceStatus.Published: service.Publish(); break;
            case ProviderServiceStatus.Paused: service.Pause(); break;
            case ProviderServiceStatus.Archived: service.Archive(); break;
            default: return Result.Failure<ProviderServiceDto>("Estado de servicio invalido.");
        }

        repository.UpdateService(service);
        if (auditLog is not null)
        {
            var action = request.Status switch
            {
                ProviderServiceStatus.Published => AuditAction.ProviderServicePublished,
                ProviderServiceStatus.Paused => AuditAction.ProviderServicePaused,
                _ => AuditAction.ProviderServiceArchived,
            };
            await auditLog.AddAsync(AuditLogEntry.Create(
                request.OwnerUserId, action, "ProviderService", service.Id.ToString(), request.Status.ToString()), ct);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderServiceDto.FromDomain(service));
    }
}

public sealed record UpdateProviderServiceCommand(
    Guid OwnerUserId,
    Guid ProviderServiceId,
    string Name,
    string Description,
    ServiceModality Modality,
    int DurationMinutes,
    decimal PriceCrc,
    int Capacity) : IRequest<Result<ProviderServiceDto>>;

public sealed class UpdateProviderServiceCommandValidator : AbstractValidator<UpdateProviderServiceCommand>
{
    public UpdateProviderServiceCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(150);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(1000);
        RuleFor(command => command.DurationMinutes).InclusiveBetween(15, 1_440);
        RuleFor(command => command.PriceCrc).GreaterThanOrEqualTo(0);
        RuleFor(command => command.Capacity).InclusiveBetween(1, 100);
    }
}

public sealed class UpdateProviderServiceCommandHandler(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProviderServiceCommand, Result<ProviderServiceDto>>
{
    public async Task<Result<ProviderServiceDto>> Handle(UpdateProviderServiceCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (provider is null || service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<ProviderServiceDto>("Servicio no encontrado.");
        try
        {
            service.Update(request.Name, request.Description, request.Modality, request.DurationMinutes, request.PriceCrc, request.Capacity);
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<ProviderServiceDto>(exception.Message);
        }
        repository.UpdateService(service);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderServiceDto.FromDomain(service));
    }
}

public sealed record GetMyProviderServicesQuery(Guid OwnerUserId)
    : IRequest<Result<IReadOnlyList<ProviderServiceDto>>>;

public sealed class GetMyProviderServicesQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetMyProviderServicesQuery, Result<IReadOnlyList<ProviderServiceDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderServiceDto>>> Handle(GetMyProviderServicesQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<IReadOnlyList<ProviderServiceDto>>("Proveedor no encontrado.");
        var services = await repository.GetServicesByProviderAsync(provider.Id, ct);
        return Result.Success<IReadOnlyList<ProviderServiceDto>>(services.Select(ProviderServiceDto.FromDomain).ToList());
    }
}

public sealed record GetPublicProviderServicesQuery(Guid ServiceProviderId)
    : IRequest<Result<IReadOnlyList<ProviderServiceDto>>>;

public sealed class GetPublicProviderServicesQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetPublicProviderServicesQuery, Result<IReadOnlyList<ProviderServiceDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderServiceDto>>> Handle(GetPublicProviderServicesQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByIdAsync(request.ServiceProviderId, ct);
        if (provider is null || provider.Status != ServiceProviderStatus.Active)
            return Result.Failure<IReadOnlyList<ProviderServiceDto>>("Proveedor no encontrado.");
        var services = await repository.GetPublishedServicesByProviderAsync(provider.Id, ct);
        return Result.Success<IReadOnlyList<ProviderServiceDto>>(services.Select(ProviderServiceDto.FromDomain).ToList());
    }
}

public sealed record AddServiceAvailabilityRuleCommand(
    Guid OwnerUserId,
    Guid ProviderServiceId,
    DayOfWeek DayOfWeek,
    TimeOnly StartsAtLocalTime,
    TimeOnly EndsAtLocalTime) : IRequest<Result<Guid>>;

public sealed class AddServiceAvailabilityRuleCommandValidator : AbstractValidator<AddServiceAvailabilityRuleCommand>
{
    public AddServiceAvailabilityRuleCommandValidator()
    {
        RuleFor(x => x.ProviderServiceId).NotEmpty();
        RuleFor(x => x.EndsAtLocalTime).GreaterThan(x => x.StartsAtLocalTime);
    }
}

public sealed class AddServiceAvailabilityRuleCommandHandler(
    IServiceProviderRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddServiceAvailabilityRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddServiceAvailabilityRuleCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<Guid>("Proveedor no encontrado.");

        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<Guid>("Servicio no encontrado.");

        var rule = ServiceAvailabilityRule.Create(
            service.Id, request.DayOfWeek, request.StartsAtLocalTime, request.EndsAtLocalTime);
        await repository.AddAvailabilityRuleAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(rule.Id);
    }
}

public sealed record ServiceAvailabilityRuleDto(Guid Id, int DayOfWeek, TimeOnly StartsAtLocalTime, TimeOnly EndsAtLocalTime, bool IsActive);

public sealed record GetServiceAvailabilityRulesQuery(Guid OwnerUserId, Guid ProviderServiceId)
    : IRequest<Result<IReadOnlyList<ServiceAvailabilityRuleDto>>>;

public sealed class GetServiceAvailabilityRulesQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetServiceAvailabilityRulesQuery, Result<IReadOnlyList<ServiceAvailabilityRuleDto>>>
{
    public async Task<Result<IReadOnlyList<ServiceAvailabilityRuleDto>>> Handle(GetServiceAvailabilityRulesQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (provider is null || service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<IReadOnlyList<ServiceAvailabilityRuleDto>>("Servicio no encontrado.");
        var rules = await repository.GetAvailabilityRulesAsync(service.Id, ct);
        return Result.Success<IReadOnlyList<ServiceAvailabilityRuleDto>>(rules.Select(rule => new ServiceAvailabilityRuleDto(rule.Id, (int)rule.DayOfWeek, rule.StartsAtLocalTime, rule.EndsAtLocalTime, rule.IsActive)).ToList());
    }
}

public sealed record DeactivateServiceAvailabilityRuleCommand(Guid OwnerUserId, Guid RuleId) : IRequest<Result<Unit>>;

public sealed class DeactivateServiceAvailabilityRuleCommandHandler(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateServiceAvailabilityRuleCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeactivateServiceAvailabilityRuleCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var rule = await repository.GetAvailabilityRuleByIdAsync(request.RuleId, ct);
        if (provider is null || rule is null) return Result.Failure<Unit>("Horario no encontrado.");
        var service = await repository.GetServiceByIdAsync(rule.ProviderServiceId, ct);
        if (service is null || service.ServiceProviderId != provider.Id) return Result.Failure<Unit>("Acceso denegado.");
        rule.Deactivate();
        repository.UpdateAvailabilityRule(rule);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

public sealed record AddServiceAvailabilityBlockCommand(
    Guid OwnerUserId,
    Guid ProviderServiceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Reason) : IRequest<Result<Guid>>;

public sealed class AddServiceAvailabilityBlockCommandValidator : AbstractValidator<AddServiceAvailabilityBlockCommand>
{
    public AddServiceAvailabilityBlockCommandValidator()
    {
        RuleFor(command => command.ProviderServiceId).NotEmpty();
        RuleFor(command => command.EndsAt).GreaterThan(command => command.StartsAt);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}

public sealed class AddServiceAvailabilityBlockCommandHandler(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<AddServiceAvailabilityBlockCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddServiceAvailabilityBlockCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (provider is null || service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<Guid>("Servicio no encontrado.");
        var block = ServiceAvailabilityBlock.Create(service.Id, request.StartsAt, request.EndsAt, request.Reason);
        await repository.AddAvailabilityBlockAsync(block, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(block.Id);
    }
}

public sealed record ServiceAvailabilityBlockDto(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string Reason, bool IsActive);

public sealed record GetServiceAvailabilityBlocksQuery(
    Guid OwnerUserId,
    Guid ProviderServiceId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<Result<IReadOnlyList<ServiceAvailabilityBlockDto>>>;

public sealed class GetServiceAvailabilityBlocksQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetServiceAvailabilityBlocksQuery, Result<IReadOnlyList<ServiceAvailabilityBlockDto>>>
{
    public async Task<Result<IReadOnlyList<ServiceAvailabilityBlockDto>>> Handle(GetServiceAvailabilityBlocksQuery request, CancellationToken ct)
    {
        if (request.To <= request.From || request.To > request.From.AddDays(90))
            return Result.Failure<IReadOnlyList<ServiceAvailabilityBlockDto>>("El rango debe ser valido y no superar 90 dias.");
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var service = await repository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (provider is null || service is null || service.ServiceProviderId != provider.Id)
            return Result.Failure<IReadOnlyList<ServiceAvailabilityBlockDto>>("Servicio no encontrado.");
        var blocks = await repository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, request.From, request.To, ct);
        return Result.Success<IReadOnlyList<ServiceAvailabilityBlockDto>>(blocks.Select(block => new ServiceAvailabilityBlockDto(block.Id, block.StartsAt, block.EndsAt, block.Reason, block.IsActive)).ToList());
    }
}

public sealed record DeactivateServiceAvailabilityBlockCommand(Guid OwnerUserId, Guid BlockId) : IRequest<Result<Unit>>;

public sealed class DeactivateServiceAvailabilityBlockCommandHandler(IServiceProviderRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeactivateServiceAvailabilityBlockCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeactivateServiceAvailabilityBlockCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.OwnerUserId, ct);
        var block = await repository.GetAvailabilityBlockByIdAsync(request.BlockId, ct);
        if (provider is null || block is null) return Result.Failure<Unit>("Cierre no encontrado.");
        var service = await repository.GetServiceByIdAsync(block.ProviderServiceId, ct);
        if (service is null || service.ServiceProviderId != provider.Id) return Result.Failure<Unit>("Acceso denegado.");
        block.Deactivate();
        repository.UpdateAvailabilityBlock(block);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}