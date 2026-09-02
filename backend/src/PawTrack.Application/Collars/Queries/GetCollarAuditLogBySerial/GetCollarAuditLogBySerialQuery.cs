using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarAuditLog;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarAuditLogBySerial;

public sealed record GetCollarAuditLogBySerialQuery(string Serial, int Skip = 0, int Take = 50)
    : IRequest<Result<IReadOnlyList<CollarAuditEntryDto>>>;

public sealed class GetCollarAuditLogBySerialQueryHandler(ICollarAuditRepository auditRepository)
    : IRequestHandler<GetCollarAuditLogBySerialQuery, Result<IReadOnlyList<CollarAuditEntryDto>>>
{
    public async Task<Result<IReadOnlyList<CollarAuditEntryDto>>> Handle(
        GetCollarAuditLogBySerialQuery request, CancellationToken cancellationToken)
    {
        var entries = await auditRepository.GetBySerialAsync(
            request.Serial, request.Skip, request.Take, cancellationToken);

        return Result.Success<IReadOnlyList<CollarAuditEntryDto>>(
            entries.Select(CollarAuditEntryDto.FromDomain).ToList());
    }
}
