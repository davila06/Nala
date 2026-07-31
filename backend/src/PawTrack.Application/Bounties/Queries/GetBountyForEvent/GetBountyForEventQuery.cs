using MediatR;
using PawTrack.Application.Bounties.DTOs;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bounties.Queries.GetBountyForEvent;

public sealed record GetBountyForEventQuery(Guid LostPetEventId) : IRequest<Result<BountyDto?>>;

public sealed class GetBountyForEventQueryHandler(IBountyRepository bountyRepository)
    : IRequestHandler<GetBountyForEventQuery, Result<BountyDto?>>
{
    public async Task<Result<BountyDto?>> Handle(
        GetBountyForEventQuery request,
        CancellationToken cancellationToken)
    {
        var bounty = await bountyRepository.GetByLostEventAsync(request.LostPetEventId, cancellationToken);
        return Result.Success<BountyDto?>(bounty is null ? null : BountyDto.FromDomain(bounty));
    }
}
