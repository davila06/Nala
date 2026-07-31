using MediatR;
using PawTrack.Application.Bounties.DTOs;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bounties.Commands.ReleaseBounty;

public sealed record ReleaseBountyCommand(Guid BountyId, Guid RequestingUserId) : IRequest<Result<BountyDto>>;

public sealed class ReleaseBountyCommandHandler(
    IBountyRepository bountyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReleaseBountyCommand, Result<BountyDto>>
{
    public async Task<Result<BountyDto>> Handle(
        ReleaseBountyCommand request,
        CancellationToken cancellationToken)
    {
        var bounty = await bountyRepository.GetByIdAsync(request.BountyId, cancellationToken);
        if (bounty is null)
            return Result.Failure<BountyDto>("Bounty not found.");

        if (bounty.OwnerId != request.RequestingUserId)
            return Result.Failure<BountyDto>("Only the pet owner can release the bounty.");

        if (bounty.Status != BountyStatus.Claimed)
            return Result.Failure<BountyDto>("Bounty must be in Claimed status to release.");

        bounty.Release();
        bountyRepository.Update(bounty);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BountyDto.FromDomain(bounty));
    }
}
