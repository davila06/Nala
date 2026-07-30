using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Commands.ReactivatePet;

public sealed class ReactivatePetCommandHandler(
    IPetRepository petRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReactivatePetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        ReactivatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<bool>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        var reactivateResult = pet.Reactivate();

        if (reactivateResult.IsFailure)
            return reactivateResult;

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
