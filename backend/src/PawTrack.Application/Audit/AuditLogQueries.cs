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
    int Take = 100) : IRequest<Result<IReadOnlyList<AuditLogEntryDto>>>;

public sealed class GetAuditLogQueryHandler(IAuditLogRepository repo)
    : IRequestHandler<GetAuditLogQuery, Result<IReadOnlyList<AuditLogEntryDto>>>
{
    public async Task<Result<IReadOnlyList<AuditLogEntryDto>>> Handle(
        GetAuditLogQuery request, CancellationToken ct)
    {
        IReadOnlyList<AuditLogEntry> entries;

        if (!string.IsNullOrWhiteSpace(request.EntityType) && !string.IsNullOrWhiteSpace(request.EntityId))
            entries = await repo.GetByEntityAsync(request.EntityType, request.EntityId, ct);
        else
            entries = await repo.GetRecentAsync(Math.Clamp(request.Take, 1, 500), ct);

        return Result.Success<IReadOnlyList<AuditLogEntryDto>>(
            entries.Select(AuditLogEntryDto.FromDomain).ToList());
    }
}
