using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.DTOs;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Municipalities.Commands.UploadCapturePhoto;

// ── Command — Full+ ───────────────────────────────────────────────────────────

public sealed record UploadCapturePhotoCommand(
    Guid RequestingUserId,
    Guid AnimalId,
    byte[] PhotoBytes,
    string ContentType) : IRequest<Result<string>>;

public sealed class UploadCapturePhotoCommandHandler(
    ICapturedAnimalRepository capturedAnimalRepo,
    IMunicipalSubscriptionService subscriptionService,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadCapturePhotoCommand, Result<string>>
{
    private const string Container = "municipal-photos";

    public async Task<Result<string>> Handle(
        UploadCapturePhotoCommand request, CancellationToken ct)
    {
        if (!await subscriptionService.IsFullOrAboveAsync(request.RequestingUserId, ct))
            return Result.Failure<string>("La carga de fotos requiere el plan Full o Red Regional.");

        var animal = await capturedAnimalRepo.GetByIdAsync(request.AnimalId, ct);
        if (animal is null)
            return Result.Failure<string>("Registro de animal no encontrado.");

        var ext = request.ContentType.Contains("png") ? "png" : "jpg";
        var blobName = $"{request.AnimalId}/{Guid.CreateVersion7()}.{ext}";
        using var stream = new MemoryStream(request.PhotoBytes);
        var url = await blobStorage.UploadAsync(Container, blobName, stream, request.ContentType, ct);

        animal.SetPhotoUrl(url);
        capturedAnimalRepo.Update(animal);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(url);
    }
}
