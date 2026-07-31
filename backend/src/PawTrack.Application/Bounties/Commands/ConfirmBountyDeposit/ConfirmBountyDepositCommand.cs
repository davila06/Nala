using MediatR;
using PawTrack.Application.Bounties.DTOs;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;

public sealed record ConfirmBountyDepositCommand(string DepositReference) : IRequest<Result<BountyDto>>;

public sealed class ConfirmBountyDepositCommandHandler(
    IBountyRepository bountyRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmBountyDepositCommand, Result<BountyDto>>
{
    public async Task<Result<BountyDto>> Handle(
        ConfirmBountyDepositCommand request,
        CancellationToken cancellationToken)
    {
        // Look up by deposit reference
        var bounty = await bountyRepository.GetByDepositReferenceAsync(request.DepositReference, cancellationToken);
        if (bounty is null)
            return Result.Failure<BountyDto>("Bounty deposit reference not found.");

        bounty.ConfirmDeposit();
        bountyRepository.Update(bounty);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BountyDto.FromDomain(bounty));
    }
}
