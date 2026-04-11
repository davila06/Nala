using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Sightings;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Application.Sightings.Commands.ReportSighting;

public sealed class ReportSightingCommandHandler(
    ISightingRepository sightingRepository,
    IPetRepository petRepository,
    ILostPetRepository lostPetRepository,
    IUserRepository userRepository,
    IUserLocationRepository userLocationRepository,
    INotificationRepository notificationRepository,
    IBlobStorageService blobStorageService,
    IImageProcessor imageProcessor,
    IPiiScrubber piiScrubber,
    INotificationDispatcher notificationDispatcher,
    IOptions<ResolveCheckSettings> settings,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReportSightingCommand, Result<string>>
{
    private const string SightingPhotosContainer = "sighting-photos";

    public async Task<Result<string>> Handle(
        ReportSightingCommand request, CancellationToken cancellationToken)
    {
        var cfg = settings.Value;
        // Validate pet exists
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<string>("Pet not found.");

        // Look up any active lost report to link the sighting
        var activeLostReport = await lostPetRepository.GetActiveByPetIdAsync(
            request.PetId, cancellationToken);

        // Scrub PII from raw note before storing
        var sanitisedNote = piiScrubber.Scrub(request.RawNote);

        var sighting = Sighting.Create(
            request.PetId,
            activeLostReport?.Id,
            request.Lat,
            request.Lng,
            sanitisedNote,
            request.SightedAt);

        // Upload photo if provided
        if (request.PhotoStream is not null && request.PhotoContentType is not null)
        {
            using var ms = new MemoryStream();
            await request.PhotoStream.CopyToAsync(ms, cancellationToken);
            var rawBytes = ms.ToArray();

            var safeBytes = await imageProcessor.ResizeAsync(rawBytes, 800, cancellationToken);

            var blobName = $"{sighting.Id}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg";
            using var safeStream = new MemoryStream(safeBytes);
            var photoUrl = await blobStorageService.UploadAsync(
                SightingPhotosContainer,
                blobName,
                safeStream,
                "image/jpeg",
                cancellationToken);

            sighting.SetPhoto(photoUrl);
        }

        await sightingRepository.AddAsync(sighting, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Notify owner if the pet has an active lost report
        if (activeLostReport is not null)
        {
            var owner = await userRepository.GetByIdAsync(pet.OwnerId, cancellationToken);
            if (owner is not null)
            {
                await notificationDispatcher.DispatchSightingAlertAsync(
                    owner.Id,
                    owner.Email,
                    owner.Name,
                    pet.Name,
                    sighting.Id.ToString(),
                    cancellationToken);
            }

            var ownerLocation = await userLocationRepository.GetByUserIdAsync(pet.OwnerId, cancellationToken);
            if (ownerLocation is not null)
            {
                var distance = GeoHelper.DistanceMetres(
                    ownerLocation.Lat,
                    ownerLocation.Lng,
                    request.Lat,
                    request.Lng);

                if (distance <= cfg.SightingHomeProximityMetres)
                {
                    var lostEventId = activeLostReport.Id.ToString();
                    var hasRecentResolvePrompt = await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                        pet.OwnerId,
                        NotificationType.ResolveCheck,
                        lostEventId,
                        TimeSpan.FromHours(cfg.DedupWindowHours),
                        cancellationToken);

                    if (!hasRecentResolvePrompt)
                    {
                        var resolveCheck = Notification.Create(
                            pet.OwnerId,
                            NotificationType.ResolveCheck,
                            $"¿Encontraste a {pet.Name}?",
                            "Se recibió un avistamiento muy cerca de tu hogar. ¿Deseas cerrar el reporte?",
                            lostEventId);

                        await notificationRepository.AddAsync(resolveCheck, cancellationToken);
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }

        return Result.Success(sighting.Id.ToString());
    }
}
