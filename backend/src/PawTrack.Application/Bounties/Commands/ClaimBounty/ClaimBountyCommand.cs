using MediatR;
using PawTrack.Application.Bounties.DTOs;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bounties.Commands.ClaimBounty;

/// <summary>
/// Transitions an Active bounty to Claimed after the rescuer's HandoverCode is verified.
/// Called automatically by VerifyHandoverCodeCommand when a bounty exists for the event.
/// </summary>
public sealed record ClaimBountyCommand(
    Guid LostPetEventId,
    Guid ClaimedByUserId,
    Guid? SightingId = null) : IRequest<Result<BountyDto?>>;

public sealed class ClaimBountyCommandHandler(
    IBountyRepository bountyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClaimBountyCommand, Result<BountyDto?>>
{
    public async Task<Result<BountyDto?>> Handle(
        ClaimBountyCommand request, CancellationToken cancellationToken)
    {
        var bounty = await bountyRepository.GetByLostEventAsync(request.LostPetEventId, cancellationToken);

        // No bounty for this event — not an error, just nothing to claim
        if (bounty is null || bounty.Status != BountyStatus.Active)
            return Result.Success<BountyDto?>(null);

        bounty.Claim(request.SightingId ?? Guid.Empty, request.ClaimedByUserId);
        bountyRepository.Update(bounty);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<BountyDto?>(BountyDto.FromDomain(bounty));
    }
}
