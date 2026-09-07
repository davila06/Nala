using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderBookingDto(
    Guid Id,
    Guid ServiceProviderId,
    Guid ProviderServiceId,
    Guid PetId,
    string ServiceName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    decimal PriceCrc,
    int Quantity,
    string Status);

public sealed record CreateProviderBookingCommand(
    Guid CustomerUserId,
    Guid ProviderServiceId,
    Guid PetId,
    DateTimeOffset StartsAt,
    int Quantity,
    string? CustomerNote) : IRequest<Result<ProviderBookingDto>>;

public sealed class CreateProviderBookingCommandValidator : AbstractValidator<CreateProviderBookingCommand>
{
    public CreateProviderBookingCommandValidator()
    {
        RuleFor(x => x.ProviderServiceId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.StartsAt).GreaterThan(DateTimeOffset.UtcNow);
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
        RuleFor(x => x.CustomerNote).MaximumLength(500);
    }
}

public sealed class CreateProviderBookingCommandHandler(
    IServiceProviderRepository providerRepository,
    IPetRepository petRepository,
    IUnitOfWork unitOfWork,
    INotificationRepository? notificationRepository = null)
    : IRequestHandler<CreateProviderBookingCommand, Result<ProviderBookingDto>>
{
    public async Task<Result<ProviderBookingDto>> Handle(CreateProviderBookingCommand request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.CustomerUserId)
            return Result.Failure<ProviderBookingDto>("Mascota no encontrada.");

        var service = await providerRepository.GetServiceByIdAsync(request.ProviderServiceId, ct);
        if (service is null || service.Status != ProviderServiceStatus.Published)
            return Result.Failure<ProviderBookingDto>("Servicio no disponible.");
        if (request.Quantity > service.Capacity)
            return Result.Failure<ProviderBookingDto>("La cantidad excede la capacidad del servicio.");

        var provider = await providerRepository.GetByIdAsync(service.ServiceProviderId, ct);
        if (provider is null || provider.Status != ServiceProviderStatus.Active)
            return Result.Failure<ProviderBookingDto>("Proveedor no disponible.");

        var endsAt = request.StartsAt.AddMinutes(service.DurationMinutes);
        var availabilityRules = await providerRepository.GetActiveAvailabilityRulesAsync(service.Id, ct);
        if (availabilityRules.Count == 0 || !availabilityRules.Any(rule => rule.Covers(request.StartsAt, endsAt)))
            return Result.Failure<ProviderBookingDto>("El horario seleccionado no esta disponible.");
        var blocks = await providerRepository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, request.StartsAt, endsAt, ct);
        if (blocks.Any(block => block.Blocks(request.StartsAt, endsAt)))
            return Result.Failure<ProviderBookingDto>("El horario seleccionado no esta disponible.");

        var booking = ProviderBooking.Request(
            provider.Id, service.Id, request.CustomerUserId, pet.Id, service.Name,
            request.StartsAt, service.DurationMinutes, service.PriceCrc, request.Quantity,
            request.CustomerNote);
        if (!await providerRepository.TryAddBookingAsync(booking, service.Capacity, ct))
            return Result.Failure<ProviderBookingDto>("El horario seleccionado ya no tiene capacidad disponible.");
        if (notificationRepository is not null)
        {
            await notificationRepository.AddAsync(Notification.Create(
                provider.UserId,
                NotificationType.ProviderBookingUpdate,
                "Nueva solicitud de reserva",
                $"Tienes una solicitud para {service.Name} el {booking.StartsAt:dd/MM/yyyy HH:mm}.",
                booking.Id.ToString()), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        return Result.Success(ToDto(booking));
    }

    internal static ProviderBookingDto ToDto(ProviderBooking booking) => new(
        booking.Id, booking.ServiceProviderId, booking.ProviderServiceId, booking.PetId,
        booking.ServiceName, booking.StartsAt, booking.EndsAt, booking.PriceCrc,
        booking.Quantity, booking.Status.ToString());
}

public sealed record GetMyProviderBookingsQuery(Guid CustomerUserId, int Page, int PageSize)
    : IRequest<Result<IReadOnlyList<ProviderBookingDto>>>;

public sealed class GetMyProviderBookingsQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetMyProviderBookingsQuery, Result<IReadOnlyList<ProviderBookingDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderBookingDto>>> Handle(GetMyProviderBookingsQuery request, CancellationToken ct)
    {
        var size = Math.Clamp(request.PageSize, 1, 100);
        var bookings = await repository.GetBookingsByCustomerAsync(request.CustomerUserId, (Math.Max(1, request.Page) - 1) * size, size, ct);
        return Result.Success<IReadOnlyList<ProviderBookingDto>>(bookings.Select(CreateProviderBookingCommandHandler.ToDto).ToList());
    }
}

public sealed record GetIncomingProviderBookingsQuery(Guid ProviderOwnerUserId, int Page, int PageSize)
    : IRequest<Result<IReadOnlyList<ProviderBookingDto>>>;

public sealed class GetIncomingProviderBookingsQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetIncomingProviderBookingsQuery, Result<IReadOnlyList<ProviderBookingDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderBookingDto>>> Handle(GetIncomingProviderBookingsQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.ProviderOwnerUserId, ct);
        if (provider is null) return Result.Failure<IReadOnlyList<ProviderBookingDto>>("Proveedor no encontrado.");
        var size = Math.Clamp(request.PageSize, 1, 100);
        var bookings = await repository.GetBookingsByProviderAsync(provider.Id, (Math.Max(1, request.Page) - 1) * size, size, ct);
        return Result.Success<IReadOnlyList<ProviderBookingDto>>(bookings.Select(CreateProviderBookingCommandHandler.ToDto).ToList());
    }
}

public sealed record UpdateProviderBookingStatusCommand(Guid ActorUserId, Guid BookingId, ProviderBookingStatus TargetStatus, string? Reason)
    : IRequest<Result<ProviderBookingDto>>;

public sealed class UpdateProviderBookingStatusCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    INotificationRepository notificationRepository)
    : IRequestHandler<UpdateProviderBookingStatusCommand, Result<ProviderBookingDto>>
{
    public async Task<Result<ProviderBookingDto>> Handle(UpdateProviderBookingStatusCommand request, CancellationToken ct)
    {
        var booking = await repository.GetBookingByIdAsync(request.BookingId, ct);
        if (booking is null) return Result.Failure<ProviderBookingDto>("Reserva no encontrada.");
        var isCustomer = booking.CustomerUserId == request.ActorUserId;
        var actorProvider = await repository.GetByUserIdAsync(request.ActorUserId, ct);
        var isProvider = actorProvider?.Id == booking.ServiceProviderId;
        try
        {
            switch (request.TargetStatus)
            {
                case ProviderBookingStatus.Confirmed when isProvider: booking.Confirm(); break;
                case ProviderBookingStatus.InProgress when isProvider: booking.Start(); break;
                case ProviderBookingStatus.Completed when isProvider: booking.Complete(); break;
                case ProviderBookingStatus.NoShow when isProvider: booking.MarkNoShow(); break;
                case ProviderBookingStatus.CancelledByCustomer when isCustomer: booking.CancelByCustomer(request.Reason ?? string.Empty); break;
                case ProviderBookingStatus.CancelledByProvider when isProvider: booking.CancelByProvider(request.Reason ?? string.Empty); break;
                default: return Result.Failure<ProviderBookingDto>("No tienes permiso para esta transicion.");
            }
        }
        catch (InvalidOperationException exception)
        {
            return Result.Failure<ProviderBookingDto>(exception.Message);
        }

        repository.UpdateBooking(booking);
        var action = request.TargetStatus == ProviderBookingStatus.Confirmed ? AuditAction.ProviderBookingConfirmed
            : request.TargetStatus == ProviderBookingStatus.Completed ? AuditAction.ProviderBookingCompleted
            : AuditAction.ProviderBookingCancelled;
        await auditLog.AddAsync(AuditLogEntry.Create(request.ActorUserId, action, "ProviderBooking", booking.Id.ToString()), ct);
        var recipientUserId = booking.CustomerUserId;
        if (isCustomer)
        {
            var bookingProvider = await repository.GetByIdAsync(booking.ServiceProviderId, ct);
            if (bookingProvider is not null) recipientUserId = bookingProvider.UserId;
        }
        await notificationRepository.AddAsync(Notification.Create(
            recipientUserId,
            NotificationType.ProviderBookingUpdate,
            "Actualizacion de reserva",
            BookingStatusMessage(booking, isCustomer),
            booking.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(CreateProviderBookingCommandHandler.ToDto(booking));
    }

    private static string BookingStatusMessage(ProviderBooking booking, bool changedByCustomer) => booking.Status switch
    {
        ProviderBookingStatus.Confirmed => $"Tu reserva de {booking.ServiceName} fue confirmada.",
        ProviderBookingStatus.InProgress => $"Tu reserva de {booking.ServiceName} esta en curso.",
        ProviderBookingStatus.Completed => $"Tu reserva de {booking.ServiceName} fue completada.",
        ProviderBookingStatus.CancelledByProvider => $"El proveedor cancelo tu reserva de {booking.ServiceName}.",
        ProviderBookingStatus.CancelledByCustomer when changedByCustomer => $"Una reserva de {booking.ServiceName} fue cancelada por el cliente.",
        ProviderBookingStatus.CancelledByCustomer => $"Tu reserva de {booking.ServiceName} fue cancelada.",
        _ => $"Tu reserva de {booking.ServiceName} fue actualizada.",
    };
}

public sealed record RescheduleProviderBookingCommand(Guid CustomerUserId, Guid BookingId, DateTimeOffset StartsAt)
    : IRequest<Result<ProviderBookingDto>>;

public sealed class RescheduleProviderBookingCommandValidator : AbstractValidator<RescheduleProviderBookingCommand>
{
    public RescheduleProviderBookingCommandValidator()
    {
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.StartsAt).GreaterThan(DateTimeOffset.UtcNow);
    }
}

public sealed class RescheduleProviderBookingCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork,
    INotificationRepository notificationRepository)
    : IRequestHandler<RescheduleProviderBookingCommand, Result<ProviderBookingDto>>
{
    public async Task<Result<ProviderBookingDto>> Handle(RescheduleProviderBookingCommand request, CancellationToken ct)
    {
        var booking = await repository.GetBookingByIdAsync(request.BookingId, ct);
        if (booking is null || booking.CustomerUserId != request.CustomerUserId)
            return Result.Failure<ProviderBookingDto>("Reserva no encontrada.");
        var service = await repository.GetServiceByIdAsync(booking.ProviderServiceId, ct);
        if (service is null || service.Status != ProviderServiceStatus.Published)
            return Result.Failure<ProviderBookingDto>("Servicio no disponible.");
        var endsAt = request.StartsAt.AddMinutes(service.DurationMinutes);
        var rules = await repository.GetActiveAvailabilityRulesAsync(service.Id, ct);
        var blocks = await repository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, request.StartsAt, endsAt, ct);
        if (!rules.Any(rule => rule.Covers(request.StartsAt, endsAt)) || blocks.Any(block => block.Blocks(request.StartsAt, endsAt)))
            return Result.Failure<ProviderBookingDto>("El horario seleccionado no esta disponible.");

        if (booking.Status is not (ProviderBookingStatus.Requested or ProviderBookingStatus.Confirmed))
            return Result.Failure<ProviderBookingDto>("Solo una reserva solicitada o confirmada puede reprogramarse.");
        if (!await repository.TryRescheduleBookingAsync(booking, request.StartsAt, service.DurationMinutes, service.Capacity, ct))
            return Result.Failure<ProviderBookingDto>("El horario seleccionado ya no tiene capacidad disponible.");

        await auditLog.AddAsync(AuditLogEntry.Create(request.CustomerUserId, AuditAction.ProviderBookingRescheduled, "ProviderBooking", booking.Id.ToString()), ct);
        var provider = await repository.GetByIdAsync(booking.ServiceProviderId, ct);
        if (provider is not null)
            await notificationRepository.AddAsync(Notification.Create(
                provider.UserId, NotificationType.ProviderBookingUpdate,
                "Reserva reprogramada", $"Una reserva de {booking.ServiceName} fue reprogramada para {booking.StartsAt:dd/MM/yyyy HH:mm}.", booking.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(CreateProviderBookingCommandHandler.ToDto(booking));
    }
}