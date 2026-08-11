using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.SearchRadius;
using PawTrack.Application.Subscriptions.Services;
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
    ISubscriptionService subscriptionService,
    IClinicRepository clinicRepository,
    INeighborAlertRepository neighborAlertRepository,
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
            rewardNote: request.RewardNote,
            currencyCode: request.CurrencyCode);

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
            var tierMultiplier = await subscriptionService.GetAlertRadiusMultiplierAsync(
                request.RequestingUserId, cancellationToken);

            var alertRadiusMetres = searchRadiusCalculator.Calculate(
                pet.Species,
                pet.Breed,
                lostPetEvent.LastSeenAt,
                tierMultiplier);

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

            // Notify Partner clinics near the lost-pet location (fire-and-forget per clinic)
            var partnerClinics = await clinicRepository.GetFeaturedNearAsync(
                (double)lostPetEvent.LastSeenLat.Value,
                (double)lostPetEvent.LastSeenLng.Value,
                radiusKm: 15,
                cancellationToken);

            foreach (var clinic in partnerClinics)
            {
                _ = notificationDispatcher.DispatchLostPetAlertToClinicAsync(
                    clinic.UserId,
                    lostPetEvent.Id,
                    pet.Name,
                    (double)lostPetEvent.LastSeenLat.Value,
                    (double)lostPetEvent.LastSeenLng.Value,
                    cancellationToken);
            }

            // Notify Guardia Vecinal neighbors within their configured radius
            var neighbors = await neighborAlertRepository.GetActiveInRadiusAsync(
                lostPetEvent.LastSeenLat.Value,
                lostPetEvent.LastSeenLng.Value,
                radiusMeters: 2000, // query beyond max individual radius to cover all configs
                cancellationToken);

            foreach (var neighbor in neighbors)
            {
                // Skip the owner themselves
                if (neighbor.UserId == request.RequestingUserId) continue;
                // Only notify if the loss coords fall within that neighbor's specific radius
                var distM = PawTrack.Application.Common.GeoHelper.DistanceMetres(
                    lostPetEvent.LastSeenLat.Value,
                    lostPetEvent.LastSeenLng.Value,
                    (double)neighbor.Lat,
                    (double)neighbor.Lng);
                if (distM > neighbor.RadiusMeters) continue;

                _ = notificationDispatcher.DispatchNeighborLostPetAlertAsync(
                    neighbor.UserId,
                    pet.Name,
                    pet.Species.ToString(),
                    lostPetEvent.Id.ToString(),
                    lostPetEvent.LastSeenLat.Value,
                    lostPetEvent.LastSeenLng.Value,
                    cancellationToken);
            }
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
