using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Commands.DeletePet;

public sealed class DeletePetCommandHandler(
    IPetRepository petRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePetCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeletePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<bool>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        // Remove photo from Blob Storage before deleting the record
        if (!string.IsNullOrEmpty(pet.PhotoUrl))
            await blobStorage.DeleteAsync(pet.PhotoUrl, cancellationToken);

        petRepository.Delete(pet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
