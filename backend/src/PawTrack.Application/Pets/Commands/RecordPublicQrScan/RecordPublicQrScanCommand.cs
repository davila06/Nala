using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Application.Pets.Commands.RecordPublicQrScan;

public sealed record RecordPublicQrScanCommand(
    Guid PetId,
    Guid? ScannedByUserId,
    string? IpAddress,
    string? UserAgent,
    string? CountryCode,
    string? CityName,
    DateTimeOffset ScannedAt,
    double? ScanLat,
    double? ScanLng)
    : IRequest<Result<bool>>;

public sealed class RecordPublicQrScanCommandHandler(
    IPetRepository petRepository,
    IQrScanEventRepository qrScanEventRepository,
    ILostPetRepository lostPetRepository,
    IUserLocationRepository userLocationRepository,
    INotificationRepository notificationRepository,
    IOptions<ResolveCheckSettings> settings,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RecordPublicQrScanCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RecordPublicQrScanCommand request,
        CancellationToken cancellationToken)
    {
        var cfg             = settings.Value;
        var silenceThreshold    = TimeSpan.FromHours(cfg.SilenceThresholdHours);
        var dedupWindow         = TimeSpan.FromHours(cfg.DedupWindowHours);

        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<bool>("Pet not found.");

        var scanDate = DateOnly.FromDateTime(request.ScannedAt.UtcDateTime);
        var isFirstScanToday = !await qrScanEventRepository.HasScanForPetOnDateAsync(
            request.PetId,
            scanDate,
            cancellationToken);

        var scanEvent = QrScanEvent.Create(
            request.PetId,
            request.ScannedByUserId?.ToString(),
            HashIp(request.IpAddress),
            request.UserAgent,
            request.CountryCode,
            request.CityName,
            request.ScannedAt);

        await qrScanEventRepository.AddAsync(scanEvent, cancellationToken);

        var activeLostReport = pet.Status == PetStatus.Lost
            ? await lostPetRepository.GetActiveByPetIdAsync(pet.Id, cancellationToken)
            : null;

        if (pet.Status == PetStatus.Lost && isFirstScanToday)
        {
            var cityPart = string.IsNullOrWhiteSpace(request.CityName)
                ? "una ubicación aproximada"
                : request.CityName.Trim();

            var notification = Notification.Create(
                pet.OwnerId,
                NotificationType.SystemMessage,
                $"Nuevo escaneo QR de {pet.Name}",
                $"Se registró un escaneo del QR hoy desde {cityPart}.",
                pet.Id.ToString());

            await notificationRepository.AddAsync(notification, cancellationToken);
        }

        if (activeLostReport is not null)
        {
            var shouldPromptResolve =
                await IsNearOwnerHomeAsync(request, pet.OwnerId, cfg.HomeProximityMetres, cancellationToken)
                || await IsFirstScanAfterSilenceAsync(request, activeLostReport, silenceThreshold, cancellationToken);

            if (shouldPromptResolve)
            {
                var lostEventId = activeLostReport.Id.ToString();

                var alreadySentRecently = await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                    pet.OwnerId,
                    NotificationType.ResolveCheck,
                    lostEventId,
                    within: dedupWindow,
                    cancellationToken);

                if (!alreadySentRecently)
                {
                    var resolveCheck = Notification.Create(
                        pet.OwnerId,
                        NotificationType.ResolveCheck,
                        $"¿Encontraste a {pet.Name}?",
                        "Detectamos señales recientes cerca de tu contexto. ¿Deseas cerrar el reporte ahora?",
                        lostEventId);

                    await notificationRepository.AddAsync(resolveCheck, cancellationToken);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(true);
    }

    private async Task<bool> IsNearOwnerHomeAsync(
        RecordPublicQrScanCommand request,
        Guid ownerId,
        int proximityMetres,
        CancellationToken cancellationToken)
    {
        if (!request.ScanLat.HasValue || !request.ScanLng.HasValue)
            return false;

        var ownerLocation = await userLocationRepository.GetByUserIdAsync(ownerId, cancellationToken);
        if (ownerLocation is null)
            return false;

        var distance = GeoHelper.DistanceMetres(
            ownerLocation.Lat,
            ownerLocation.Lng,
            request.ScanLat.Value,
            request.ScanLng.Value);

        return distance <= proximityMetres;
    }

    private async Task<bool> IsFirstScanAfterSilenceAsync(
        RecordPublicQrScanCommand request,
        Domain.LostPets.LostPetEvent activeLostReport,
        TimeSpan silenceThreshold,
        CancellationToken cancellationToken)
    {
        var elapsed = request.ScannedAt - activeLostReport.ReportedAt;
        if (elapsed < silenceThreshold)
            return false;

        var hasScanSinceReport = await qrScanEventRepository.HasScanForPetSinceAsync(
            activeLostReport.PetId,
            activeLostReport.ReportedAt,
            cancellationToken);

        return !hasScanSinceReport;
    }

    private static string? HashIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return null;

        var inputBytes = Encoding.UTF8.GetBytes(ipAddress.Trim());
        var hashBytes = SHA256.HashData(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}
