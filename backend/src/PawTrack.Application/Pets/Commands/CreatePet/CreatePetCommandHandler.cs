using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.Commands.CreatePet;

public sealed class CreatePetCommandHandler(
    IPetRepository petRepository,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IEntitlementService entitlementService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePetCommand, Result<string>>
{
    private const string PetPhotosContainer = "pet-photos";

    public async Task<Result<string>> Handle(
        CreatePetCommand request, CancellationToken cancellationToken)
    {
        var decision = await entitlementService.AuthorizeAsync(
            request.OwnerId,
            "MaxPets",
            1m,
            new EntitlementContext("pet", null, request.OwnerId),
            cancellationToken);
        var limit = decision.Limit;
        if (!decision.Allowed || limit is not null)
        {
            var count = await petRepository.CountByOwnerAsync(request.OwnerId, cancellationToken);
            if (!decision.Allowed || count >= limit)
                return Result.Failure<string>(
                    $"Tu plan permite hasta {limit ?? 0} mascota(s). Actualiza tu plan para registrar más.");
        }

        var pet = Pet.Create(
            request.OwnerId,
            request.Name,
            request.Species,
            request.Breed,
            request.BirthDate);

        if (request.PhotoBytes is { Length: > 0 })
        {
            var resized = await imageProcessor.ResizeAsync(request.PhotoBytes, 800, cancellationToken);
            var blobName = $"{pet.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{BlobHelper.SanitizeFileName(request.PhotoFileName)}";
            using var stream = new MemoryStream(resized);

            var photoUrl = await blobStorage.UploadAsync(
                PetPhotosContainer, blobName, stream, "image/jpeg", cancellationToken);

            pet.SetPhoto(photoUrl);
        }

        await petRepository.AddAsync(pet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(pet.Id.ToString());
    }
}
