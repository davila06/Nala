using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.SearchRadius;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.Commands.ReportLostPet;

public sealed class ReportLostPetCommandHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    INotificationDispatcher notificationDispatcher,
    IBlobStorageService blobStorage,
    IImageProcessor imageProcessor,
    IUnitOfWork unitOfWork,
    ILostPetSearchRadiusCalculator searchRadiusCalculator)
    : IRequestHandler<ReportLostPetCommand, Result<string>>
{
    private const string LostPetPhotosContainer = "lost-pet-photos";

    public async Task<Result<string>> Handle(
        ReportLostPetCommand request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);

        if (pet is null)
            return Result.Failure<string>("Pet not found.");

        if (pet.OwnerId != request.RequestingUserId)
            return Result.Failure<string>("Access denied.");

        var existingReport = await lostPetRepository.GetActiveByPetIdAsync(request.PetId, cancellationToken);
        if (existingReport is not null)
            return Result.Failure<string>("This pet already has an active lost report.");

        var lostPetEvent = LostPetEvent.Create(
            request.PetId,
            request.RequestingUserId,
            request.Description,
            request.LastSeenLat,
            request.LastSeenLng,
            request.LastSeenAt,
            publicMessage: request.PublicMessage,
            contactName: request.ContactName,
            contactPhone: request.ContactPhone,
            rewardAmount: request.RewardAmount,
            rewardNote: request.RewardNote);

        // Upload recent photo to blob storage before persisting (same pattern as CreatePetCommandHandler)
        if (request.PhotoBytes is { Length: > 0 })
        {
            var resized = await imageProcessor.ResizeAsync(request.PhotoBytes, 800, cancellationToken);
            var safeFileName = SanitizeFileName(request.PhotoFileName);
            var blobName = $"lost-reports/{lostPetEvent.Id}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{safeFileName}";

            using var stream = new MemoryStream(resized);
            var photoUrl = await blobStorage.UploadAsync(
                LostPetPhotosContainer, blobName, stream, "image/jpeg", cancellationToken);

            lostPetEvent.SetRecentPhoto(photoUrl);
        }

        pet.MarkAsLost();
        petRepository.Update(pet);

        await lostPetRepository.AddAsync(lostPetEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch notifications after the DB commit
        var owner = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
        if (owner is not null)
        {
            await notificationDispatcher.DispatchLostPetAlertAsync(
                owner.Id,
                owner.Email,
                owner.Name,
                pet.Name,
                lostPetEvent.Id.ToString(),
                cancellationToken);
        }

        // Geofenced alert — only when the report includes coordinates.
        if (lostPetEvent.LastSeenLat.HasValue && lostPetEvent.LastSeenLng.HasValue)
        {
            var alertRadiusMetres = searchRadiusCalculator.Calculate(
                pet.Species,
                pet.Breed,
                lostPetEvent.LastSeenAt);

            await notificationDispatcher.DispatchGeofencedLostPetAlertsAsync(
                lostPetEvent.Id,
                pet.Name,
                pet.Species.ToString(),
                pet.Breed,
                lostPetEvent.LastSeenLat.Value,
                lostPetEvent.LastSeenLng.Value,
                alertRadiusMetres,
                cancellationToken);

            await notificationDispatcher.DispatchVerifiedAllyAlertsAsync(
                lostPetEvent.Id,
                pet.Name,
                pet.Species.ToString(),
                pet.Breed,
                lostPetEvent.LastSeenLat.Value,
                lostPetEvent.LastSeenLng.Value,
                cancellationToken);
        }

        return Result.Success(lostPetEvent.Id.ToString());
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
