using MediatR;
using PawTrack.Application.Bounties.DTOs;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bounties.Commands.CreateBounty;

public sealed record CreateBountyCommand(
    Guid LostPetEventId,
    Guid OwnerId,
    decimal Amount,
    string CurrencyCode = "CRC") : IRequest<Result<BountyDto>>;

public sealed class CreateBountyCommandHandler(
    IBountyRepository bountyRepository,
    IPaymentService paymentService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateBountyCommand, Result<BountyDto>>
{
    public async Task<Result<BountyDto>> Handle(
        CreateBountyCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await bountyRepository.GetByLostEventAsync(request.LostPetEventId, cancellationToken);
        if (existing is not null && existing.Status is BountyStatus.Active or BountyStatus.Claimed)
            return Result.Failure<BountyDto>("An active bounty already exists for this event.");

        var reference = paymentService.GenerateReference();
        var bounty = Bounty.Create(request.LostPetEventId, request.OwnerId, request.Amount, reference);

        await bountyRepository.AddAsync(bounty, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(BountyDto.FromDomain(bounty));
    }
}
