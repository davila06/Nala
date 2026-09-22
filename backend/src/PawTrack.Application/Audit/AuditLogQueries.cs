using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Audit;

public sealed record AuditLogEntryDto(
    string Id,
    string AdminUserId,
    string Action,
    string EntityType,
    string EntityId,
    string? Details,
    DateTimeOffset PerformedAt)
{
    public static AuditLogEntryDto FromDomain(AuditLogEntry e) => new(
        e.Id.ToString(), e.AdminUserId.ToString(), e.Action.ToString(),
        e.EntityType, e.EntityId, e.Details, e.PerformedAt);
}

public sealed record GetAuditLogQuery(
    string? EntityType,
    string? EntityId,
    int Take = 100,
    Guid? ActorId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null) : IRequest<Result<IReadOnlyList<AuditLogEntryDto>>>;

public sealed class GetAuditLogQueryHandler(IAuditLogRepository repo)
    : IRequestHandler<GetAuditLogQuery, Result<IReadOnlyList<AuditLogEntryDto>>>
{
    public async Task<Result<IReadOnlyList<AuditLogEntryDto>>> Handle(
        GetAuditLogQuery request, CancellationToken ct)
    {
        if (request.From.HasValue && request.To.HasValue && request.To <= request.From)
            return Result.Failure<IReadOnlyList<AuditLogEntryDto>>("El rango de auditoría debe ser válido.");

        var entries = await repo.GetFilteredAsync(new AuditLogFilter(
            request.EntityType, request.EntityId, request.ActorId,
            request.From, request.To, Math.Clamp(request.Take, 1, 500)), ct);

        return Result.Success<IReadOnlyList<AuditLogEntryDto>>(
            entries.Select(AuditLogEntryDto.FromDomain).ToList());
    }
}
