using FluentValidation;
using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderIncidentDto(
    Guid Id,
    Guid ServiceProviderId,
    string Type,
    string Status,
    string Description,
    string? EvidenceReference,
    string? Resolution,
    string? AppealReason,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt)
{
    public static ProviderIncidentDto FromDomain(ProviderIncident incident) => new(
        incident.Id, incident.ServiceProviderId, incident.Type.ToString(), incident.Status.ToString(),
        incident.Description, incident.EvidenceReference, incident.Resolution, incident.AppealReason,
        incident.AssignedToUserId, incident.CreatedAt, incident.ResolvedAt, incident.ClosedAt);
}

public sealed record OpenProviderIncidentCommand(
    Guid ReporterUserId,
    Guid ServiceProviderId,
    ProviderIncidentType Type,
    string Description,
    string? EvidenceReference) : IRequest<Result<ProviderIncidentDto>>;

public sealed class OpenProviderIncidentCommandValidator : AbstractValidator<OpenProviderIncidentCommand>
{
    public OpenProviderIncidentCommandValidator()
    {
        RuleFor(x => x.ReporterUserId).NotEmpty();
        RuleFor(x => x.ServiceProviderId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4_000);
        RuleFor(x => x.EvidenceReference).MaximumLength(500);
    }
}

public sealed class OpenProviderIncidentCommandHandler(
    IServiceProviderRepository repository,
    IAuditLogRepository auditLog,
    IUnitOfWork unitOfWork)
    : IRequestHandler<OpenProviderIncidentCommand, Result<ProviderIncidentDto>>
{
    public async Task<Result<ProviderIncidentDto>> Handle(OpenProviderIncidentCommand request, CancellationToken ct)
    {
        var provider = await repository.GetByIdAsync(request.ServiceProviderId, ct);
        if (provider is null)
            return Result.Failure<ProviderIncidentDto>("Proveedor no encontrado.");
        if (provider.UserId != request.ReporterUserId)
            return Result.Failure<ProviderIncidentDto>("No tienes permiso para reportar este proveedor.");

        var incident = ProviderIncident.Open(
            request.ServiceProviderId, request.ReporterUserId, request.Type,
            request.Description, request.EvidenceReference);
        await repository.AddIncidentAsync(incident, ct);
        await auditLog.AddAsync(AuditLogEntry.Create(
            request.ReporterUserId, AuditAction.ProviderIncidentOpened,
            "ProviderIncident", incident.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderIncidentDto.FromDomain(incident));
    }
}

public sealed record GetMyProviderIncidentsQuery(Guid ProviderOwnerUserId, int Page = 1, int PageSize = 50)
    : IRequest<Result<IReadOnlyList<ProviderIncidentDto>>>;

public sealed class GetMyProviderIncidentsQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetMyProviderIncidentsQuery, Result<IReadOnlyList<ProviderIncidentDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderIncidentDto>>> Handle(GetMyProviderIncidentsQuery request, CancellationToken ct)
    {
        var provider = await repository.GetByUserIdAsync(request.ProviderOwnerUserId, ct);
        if (provider is null)
            return Result.Failure<IReadOnlyList<ProviderIncidentDto>>("Proveedor no encontrado.");
        var take = Math.Clamp(request.PageSize, 1, 100);
        var incidents = await repository.GetIncidentsByProviderAsync(
            provider.Id, (Math.Max(1, request.Page) - 1) * take, take, ct);
        return Result.Success<IReadOnlyList<ProviderIncidentDto>>(
            incidents.Select(ProviderIncidentDto.FromDomain).ToList());
    }
}

public sealed record GetProviderIncidentsForAdminQuery(int Page = 1, int PageSize = 50)
    : IRequest<Result<IReadOnlyList<ProviderIncidentDto>>>;

public sealed class GetProviderIncidentsForAdminQueryHandler(IServiceProviderRepository repository)
    : IRequestHandler<GetProviderIncidentsForAdminQuery, Result<IReadOnlyList<ProviderIncidentDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderIncidentDto>>> Handle(GetProviderIncidentsForAdminQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.PageSize, 1, 100);
        var incidents = await repository.GetOperationalIncidentsAsync(
            (Math.Max(1, request.Page) - 1) * take, take, ct);
        return Result.Success<IReadOnlyList<ProviderIncidentDto>>(
            incidents.Select(ProviderIncidentDto.FromDomain).ToList());
    }
}

public sealed record StartProviderIncidentInvestigationCommand(
    Guid AdminUserId, Guid IncidentId, Guid AssignedToUserId) : IRequest<Result<ProviderIncidentDto>>;

public sealed record ResolveProviderIncidentCommand(
    Guid AdminUserId, Guid IncidentId, string Resolution) : IRequest<Result<ProviderIncidentDto>>;

public sealed record AppealProviderIncidentCommand(
    Guid ReporterUserId, Guid IncidentId, string Reason) : IRequest<Result<ProviderIncidentDto>>;

public sealed record CloseProviderIncidentCommand(
    Guid AdminUserId, Guid IncidentId) : IRequest<Result<ProviderIncidentDto>>;

public sealed class StartProviderIncidentInvestigationCommandHandler(
    IServiceProviderRepository repository, IAuditLogRepository auditLog, IUnitOfWork unitOfWork)
    : IRequestHandler<StartProviderIncidentInvestigationCommand, Result<ProviderIncidentDto>>
{
    public async Task<Result<ProviderIncidentDto>> Handle(StartProviderIncidentInvestigationCommand request, CancellationToken ct)
    {
        var incident = await repository.GetIncidentByIdAsync(request.IncidentId, ct);
        if (incident is null) return Result.Failure<ProviderIncidentDto>("Incidente no encontrado.");
        try { incident.StartInvestigation(request.AssignedToUserId); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        { return Result.Failure<ProviderIncidentDto>(exception.Message); }
        return await SaveAsync(incident, request.AdminUserId, AuditAction.ProviderIncidentInvestigationStarted, auditLog, repository, unitOfWork, ct);
    }

    internal static async Task<Result<ProviderIncidentDto>> SaveAsync(
        ProviderIncident incident, Guid actorId, AuditAction action, IAuditLogRepository auditLog,
        IServiceProviderRepository repository, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        repository.UpdateIncident(incident);
        await auditLog.AddAsync(AuditLogEntry.Create(actorId, action, "ProviderIncident", incident.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderIncidentDto.FromDomain(incident));
    }
}

public sealed class ResolveProviderIncidentCommandHandler(
    IServiceProviderRepository repository, IAuditLogRepository auditLog, IUnitOfWork unitOfWork)
    : IRequestHandler<ResolveProviderIncidentCommand, Result<ProviderIncidentDto>>
{
    public async Task<Result<ProviderIncidentDto>> Handle(ResolveProviderIncidentCommand request, CancellationToken ct)
    {
        var incident = await repository.GetIncidentByIdAsync(request.IncidentId, ct);
        if (incident is null) return Result.Failure<ProviderIncidentDto>("Incidente no encontrado.");
        try { incident.Resolve(request.AdminUserId, request.Resolution); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        { return Result.Failure<ProviderIncidentDto>(exception.Message); }
        return await StartProviderIncidentInvestigationCommandHandler.SaveAsync(
            incident, request.AdminUserId, AuditAction.ProviderIncidentResolved, auditLog, repository, unitOfWork, ct);
    }
}

public sealed class AppealProviderIncidentCommandHandler(
    IServiceProviderRepository repository, IAuditLogRepository auditLog, IUnitOfWork unitOfWork)
    : IRequestHandler<AppealProviderIncidentCommand, Result<ProviderIncidentDto>>
{
    public async Task<Result<ProviderIncidentDto>> Handle(AppealProviderIncidentCommand request, CancellationToken ct)
    {
        var incident = await repository.GetIncidentByIdAsync(request.IncidentId, ct);
        if (incident is null || incident.ReportedByUserId != request.ReporterUserId)
            return Result.Failure<ProviderIncidentDto>("Incidente no encontrado.");
        try { incident.Appeal(request.Reason); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        { return Result.Failure<ProviderIncidentDto>(exception.Message); }
        return await StartProviderIncidentInvestigationCommandHandler.SaveAsync(
            incident, request.ReporterUserId, AuditAction.ProviderIncidentAppealed, auditLog, repository, unitOfWork, ct);
    }
}

public sealed class CloseProviderIncidentCommandHandler(
    IServiceProviderRepository repository, IAuditLogRepository auditLog, IUnitOfWork unitOfWork)
    : IRequestHandler<CloseProviderIncidentCommand, Result<ProviderIncidentDto>>
{
    public async Task<Result<ProviderIncidentDto>> Handle(CloseProviderIncidentCommand request, CancellationToken ct)
    {
        var incident = await repository.GetIncidentByIdAsync(request.IncidentId, ct);
        if (incident is null) return Result.Failure<ProviderIncidentDto>("Incidente no encontrado.");
        try { incident.Close(request.AdminUserId); }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        { return Result.Failure<ProviderIncidentDto>(exception.Message); }
        return await StartProviderIncidentInvestigationCommandHandler.SaveAsync(
            incident, request.AdminUserId, AuditAction.ProviderIncidentClosed, auditLog, repository, unitOfWork, ct);
    }
}
