using MediatR;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Application.ServiceProviders;

public sealed record ProviderVerificationDto(Guid Id, string Status, DateTimeOffset SubmittedAt, DateOnly? ExpiresAt, string? RejectionReason)
{
    public static ProviderVerificationDto FromDomain(ProviderVerification verification) => new(
        verification.Id, verification.Status.ToString(), verification.SubmittedAt,
        verification.ExpiresAt, verification.RejectionReason);
}

public sealed record UploadProviderVerificationDocumentCommand(
    Guid OwnerUserId,
    byte[] DocumentBytes,
    string ContentType) : IRequest<Result<ProviderVerificationDto>>;

public sealed class UploadProviderVerificationDocumentCommandHandler(
    IServiceProviderRepository providerRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadProviderVerificationDocumentCommand, Result<ProviderVerificationDto>>
{
    public async Task<Result<ProviderVerificationDto>> Handle(UploadProviderVerificationDocumentCommand request, CancellationToken ct)
    {
        if (!ProviderVerificationFilePolicy.IsAllowed(request.ContentType, request.DocumentBytes))
            return Result.Failure<ProviderVerificationDto>("Documento invalido. Solo PDF, JPEG, PNG o WebP de hasta 5 MB.");

        var provider = await providerRepository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<ProviderVerificationDto>("Proveedor no encontrado.");

        var verification = ProviderVerification.Submit(provider.Id, request.OwnerUserId);
        var extension = ProviderVerificationFilePolicy.ExtensionFor(request.ContentType);
        var blobName = $"providers/{provider.Id}/verification/{verification.Id}/document.{extension}";
        using var stream = new MemoryStream(request.DocumentBytes);
        var url = await blobStorage.UploadAsync("provider-verification", blobName, stream, request.ContentType, ct);
        verification.AttachDocument(url, request.ContentType);
        await providerRepository.AddVerificationAsync(verification, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderVerificationDto.FromDomain(verification));
    }
}

public sealed record GetMyProviderVerificationQuery(Guid OwnerUserId)
    : IRequest<Result<ProviderVerificationDto?>>;

public sealed class GetMyProviderVerificationQueryHandler(IServiceProviderRepository providerRepository)
    : IRequestHandler<GetMyProviderVerificationQuery, Result<ProviderVerificationDto?>>
{
    public async Task<Result<ProviderVerificationDto?>> Handle(GetMyProviderVerificationQuery request, CancellationToken ct)
    {
        var provider = await providerRepository.GetByUserIdAsync(request.OwnerUserId, ct);
        if (provider is null) return Result.Failure<ProviderVerificationDto?>("Proveedor no encontrado.");
        var verification = await providerRepository.GetLatestVerificationAsync(provider.Id, ct);
        return Result.Success<ProviderVerificationDto?>(verification is null ? null : ProviderVerificationDto.FromDomain(verification));
    }
}

public sealed record GetPendingProviderVerificationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<ProviderVerificationDto>>>;

public sealed class GetPendingProviderVerificationsQueryHandler(IServiceProviderRepository providerRepository)
    : IRequestHandler<GetPendingProviderVerificationsQuery, Result<IReadOnlyList<ProviderVerificationDto>>>
{
    public async Task<Result<IReadOnlyList<ProviderVerificationDto>>> Handle(GetPendingProviderVerificationsQuery request, CancellationToken ct)
    {
        var take = Math.Clamp(request.PageSize, 1, 100);
        var skip = (Math.Max(1, request.Page) - 1) * take;
        var verifications = await providerRepository.GetPendingVerificationsAsync(skip, take, ct);
        return Result.Success<IReadOnlyList<ProviderVerificationDto>>(verifications.Select(ProviderVerificationDto.FromDomain).ToList());
    }
}

public sealed record ReviewProviderVerificationCommand(
    Guid VerificationId,
    Guid AdminUserId,
    bool Approve,
    DateOnly? ExpiresAt,
    string? Reason) : IRequest<Result<ProviderVerificationDto>>;

public sealed class ReviewProviderVerificationCommandHandler(
    IServiceProviderRepository providerRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewProviderVerificationCommand, Result<ProviderVerificationDto>>
{
    public async Task<Result<ProviderVerificationDto>> Handle(ReviewProviderVerificationCommand request, CancellationToken ct)
    {
        var verification = await providerRepository.GetVerificationByIdAsync(request.VerificationId, ct);
        if (verification is null) return Result.Failure<ProviderVerificationDto>("Verificacion no encontrada.");
        var reviewResult = request.Approve
            ? verification.Verify(request.AdminUserId, request.ExpiresAt, request.Reason)
            : verification.Reject(request.AdminUserId, request.Reason ?? string.Empty);
        if (reviewResult.IsFailure) return Result.Failure<ProviderVerificationDto>(reviewResult.Errors);

        if (request.Approve)
            await providerRepository.SupersedeActiveVerificationsAsync(verification.ServiceProviderId, verification.Id, ct);
        providerRepository.UpdateVerification(verification);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.AdminUserId,
            request.Approve ? AuditAction.ProviderVerificationApproved : AuditAction.ProviderVerificationRejected,
            "ProviderVerification",
            verification.Id.ToString(),
            request.Reason), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(ProviderVerificationDto.FromDomain(verification));
    }
}

public sealed record ProviderVerificationDocumentDownloadDto(byte[] Bytes, string ContentType, string FileName);

public sealed record DownloadProviderVerificationDocumentQuery(Guid VerificationId, Guid RequestingUserId, bool IsAdmin)
    : IRequest<Result<ProviderVerificationDocumentDownloadDto>>;

public sealed class DownloadProviderVerificationDocumentQueryHandler(
    IServiceProviderRepository providerRepository,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadProviderVerificationDocumentQuery, Result<ProviderVerificationDocumentDownloadDto>>
{
    public async Task<Result<ProviderVerificationDocumentDownloadDto>> Handle(DownloadProviderVerificationDocumentQuery request, CancellationToken ct)
    {
        var verification = await providerRepository.GetVerificationByIdAsync(request.VerificationId, ct);
        if (verification is null || string.IsNullOrWhiteSpace(verification.DocumentUrl))
            return Result.Failure<ProviderVerificationDocumentDownloadDto>("Documento no disponible.");
        if (!request.IsAdmin)
        {
            var provider = await providerRepository.GetByIdAsync(verification.ServiceProviderId, ct);
            if (provider is null || provider.UserId != request.RequestingUserId)
                return Result.Failure<ProviderVerificationDocumentDownloadDto>("Acceso denegado.");
        }

        var bytes = await blobStorage.DownloadAsync(verification.DocumentUrl, ct);
        if (bytes is null) return Result.Failure<ProviderVerificationDocumentDownloadDto>("Documento no disponible.");
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId, AuditAction.ProviderVerificationDocumentDownloaded,
            "ProviderVerification", verification.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new ProviderVerificationDocumentDownloadDto(
            bytes, verification.DocumentContentType ?? "application/octet-stream", $"provider-verification-{verification.Id}"));
    }
}

file static class ProviderVerificationFilePolicy
{
    private const int MaxBytes = 5 * 1024 * 1024;

    public static bool IsAllowed(string contentType, byte[] bytes) => bytes.Length is > 3 and <= MaxBytes && contentType switch
    {
        "application/pdf" => bytes.AsSpan().StartsWith("%PDF"u8),
        "image/jpeg" => bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
        "image/png" => bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),
        "image/webp" => bytes.AsSpan().StartsWith("RIFF"u8) && bytes.Length >= 12 && bytes.AsSpan(8).StartsWith("WEBP"u8),
        _ => false,
    };

    public static string ExtensionFor(string contentType) => contentType switch
    {
        "application/pdf" => "pdf",
        "image/jpeg" => "jpg",
        "image/png" => "png",
        "image/webp" => "webp",
        _ => throw new ArgumentOutOfRangeException(nameof(contentType)),
    };
}