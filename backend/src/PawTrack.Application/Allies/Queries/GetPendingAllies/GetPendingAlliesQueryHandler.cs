using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Queries.GetPendingAllies;

public sealed class GetPendingAlliesQueryHandler(IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetPendingAlliesQuery, Result<IReadOnlyList<AllyProfileDto>>>
{
    /// <summary>
    /// Hard cap on pending-ally results returned per admin request.
    /// Prevents DoS via memory exhaustion when the pending queue grows unboundedly.
    /// </summary>
    private const int MaxResults = 200;

    public async Task<Result<IReadOnlyList<AllyProfileDto>>> Handle(
        GetPendingAlliesQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await allyProfileRepository.GetAllPendingAsync(cancellationToken);
        IReadOnlyList<AllyProfileDto> dtos = pending.Take(MaxResults).Select(AllyProfileDto.FromDomain).ToList().AsReadOnly();
        return Result.Success(dtos);
    }
}
