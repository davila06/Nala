using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.Commands.CreatePet;

public sealed class CreatePetCommandHandler(
    IPetRepository petRepository,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePetCommand, Result<string>>
{
    private const string PetPhotosContainer = "pet-photos";

    public async Task<Result<string>> Handle(
        CreatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = Pet.Create(
            request.OwnerId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate);

        if (request.PhotoBytes is { Length: > 0 })
        {
            var resized = await imageProcessor.ResizeAsync(request.PhotoBytes, 800, cancellationToken);
            var blobName = $"{pet.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{SanitizeFileName(request.PhotoFileName)}";
            using var stream = new MemoryStream(resized);

            var photoUrl = await blobStorage.UploadAsync(
                PetPhotosContainer, blobName, stream, "image/jpeg", cancellationToken);

            pet.SetPhoto(photoUrl);
        }

        await petRepository.AddAsync(pet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pet.Id.ToString());
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
