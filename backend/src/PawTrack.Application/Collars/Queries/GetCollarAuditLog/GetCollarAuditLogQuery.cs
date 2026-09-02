using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarAuditLog;

public sealed record GetCollarAuditLogQuery(Guid CollarId, Guid RequestingUserId, int Skip = 0, int Take = 50)
    : IRequest<Result<IReadOnlyList<CollarAuditEntryDto>>>;

public sealed record CollarAuditEntryDto(
    Guid Id,
    Guid? CollarId,
    string? Serial,
    Guid? UserId,
    string Event,
    string Details,
    DateTimeOffset CreatedAt)
{
    public static CollarAuditEntryDto FromDomain(CollarAuditEntry e) =>
        new(e.Id, e.CollarId, e.Serial, e.UserId, e.Event.ToString(), e.Details, e.CreatedAt);
}

public sealed class GetCollarAuditLogQueryHandler(
    ICollarRepository collarRepository,
    ICollarAuditRepository auditRepository)
    : IRequestHandler<GetCollarAuditLogQuery, Result<IReadOnlyList<CollarAuditEntryDto>>>
{
    public async Task<Result<IReadOnlyList<CollarAuditEntryDto>>> Handle(
        GetCollarAuditLogQuery request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<IReadOnlyList<CollarAuditEntryDto>>("Collar no encontrado.");

        if (collar.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<CollarAuditEntryDto>>("Access denied.");

        var entries = await auditRepository.GetByCollarIdAsync(
            request.CollarId, request.Skip, request.Take, cancellationToken);

        return Result.Success<IReadOnlyList<CollarAuditEntryDto>>(
            entries.Select(CollarAuditEntryDto.FromDomain).ToList());
    }
}
