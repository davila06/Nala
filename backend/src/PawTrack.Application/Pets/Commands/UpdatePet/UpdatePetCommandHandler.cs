using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Pets.Commands.UpdatePet;

public sealed class UpdatePetCommandHandler(
    IPetRepository petRepository,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePetCommand, Result<PetId>>
{
    private const string PetPhotosContainer = "pet-photos";

    public async Task<Result<PetId>> Handle(
        UpdatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<PetId>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<PetId>("Access denied.");

        pet.Update(request.Name, request.Species, request.Breed, request.BirthDate);

        if (request.PhotoBytes is { Length: > 0 })
        {
            if (!string.IsNullOrEmpty(pet.PhotoUrl))
                await blobStorage.DeleteAsync(pet.PhotoUrl, cancellationToken);

            var resized = await imageProcessor.ResizeAsync(request.PhotoBytes, 800, cancellationToken);
            var blobName = $"{pet.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{SanitizeFileName(request.PhotoFileName)}";
            using var stream = new MemoryStream(resized);

            var photoUrl = await blobStorage.UploadAsync(
                PetPhotosContainer, blobName, stream, "image/jpeg", cancellationToken);

            pet.SetPhoto(photoUrl);
        }

        petRepository.Update(pet);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PetId(pet.Id));
    }

    private static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return "photo.jpg";
        var clean = new string(fileName
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_')
            .ToArray());
        return string.IsNullOrEmpty(clean) ? "photo.jpg" : clean;
    }
}
