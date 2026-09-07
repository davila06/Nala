using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Commands.ManageCertificateIssuers;

public sealed record ClinicVerificationDto(
    Guid Id,
    Guid ClinicId,
    string LicenseNumberSnapshot,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? VerifiedAt,
    DateTimeOffset? ReviewedAt,
    Guid? VerifiedByAdminUserId,
    Guid? ReviewedByAdminUserId,
    DateOnly? ExpiresAt,
    bool HasDocument,
    string? RejectionReason,
    string? ReviewNotes,
    DateTimeOffset? RevalidationRequestedAt)
{
    public static ClinicVerificationDto FromDomain(ClinicVerification verification) => new(
        verification.Id,
        verification.ClinicId,
        verification.LicenseNumberSnapshot,
        verification.Status.ToString(),
        verification.SubmittedAt,
        verification.VerifiedAt,
        verification.ReviewedAt,
        verification.VerifiedByAdminUserId,
        verification.ReviewedByAdminUserId,
        verification.ExpiresAt,
        !string.IsNullOrWhiteSpace(verification.DocumentUrl),
        verification.RejectionReason,
        verification.ReviewNotes,
        verification.RevalidationRequestedAt);
}

public sealed record ClinicVeterinarianDto(
    Guid Id,
    Guid ClinicId,
    string FullName,
    string LicenseNumber,
    string Status,
    bool CanIssueCertificates,
    bool IsActive,
    bool HasDocument,
    bool HasSignature,
    DateOnly? ExpiresAt,
    string? RejectionReason,
    string? SuspensionReason)
{
    public static ClinicVeterinarianDto FromDomain(ClinicVeterinarian veterinarian) => new(
        veterinarian.Id,
        veterinarian.ClinicId,
        veterinarian.FullName,
        veterinarian.LicenseNumber,
        veterinarian.Status.ToString(),
        veterinarian.CanIssueCertificates,
        veterinarian.IsActive,
        !string.IsNullOrWhiteSpace(veterinarian.DocumentUrl),
        !string.IsNullOrWhiteSpace(veterinarian.SignatureImageUrl),
        veterinarian.ExpiresAt,
        veterinarian.RejectionReason,
        veterinarian.SuspensionReason);
}

public sealed record VerificationDocumentDownloadDto(byte[] Bytes, string FileName, string ContentType);

public sealed record GetMyClinicVeterinariansQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ClinicVeterinarianDto>>>;

public sealed class GetMyClinicVeterinariansQueryHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository)
    : IRequestHandler<GetMyClinicVeterinariansQuery, Result<IReadOnlyList<ClinicVeterinarianDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicVeterinarianDto>>> Handle(
        GetMyClinicVeterinariansQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<IReadOnlyList<ClinicVeterinarianDto>>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<IReadOnlyList<ClinicVeterinarianDto>>("Acceso denegado.");

        var veterinarians = await veterinarianRepository.GetByClinicAsync(request.ClinicId, cancellationToken);
        return Result.Success(veterinarians.Select(ClinicVeterinarianDto.FromDomain).ToList() as IReadOnlyList<ClinicVeterinarianDto>);
    }
}

public sealed record GetClinicVerificationsForAdminQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<ClinicVerificationDto>>>;

public sealed class GetClinicVerificationsForAdminQueryHandler(IClinicVerificationRepository verificationRepository)
    : IRequestHandler<GetClinicVerificationsForAdminQuery, Result<IReadOnlyList<ClinicVerificationDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicVerificationDto>>> Handle(
        GetClinicVerificationsForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var verifications = await verificationRepository.GetPendingPagedAsync((page - 1) * pageSize, pageSize, cancellationToken);
        return Result.Success(verifications.Select(ClinicVerificationDto.FromDomain).ToList() as IReadOnlyList<ClinicVerificationDto>);
    }
}

public sealed record GetClinicVeterinariansForAdminQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<ClinicVeterinarianDto>>>;

public sealed class GetClinicVeterinariansForAdminQueryHandler(IClinicVeterinarianRepository veterinarianRepository)
    : IRequestHandler<GetClinicVeterinariansForAdminQuery, Result<IReadOnlyList<ClinicVeterinarianDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicVeterinarianDto>>> Handle(
        GetClinicVeterinariansForAdminQuery request,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var veterinarians = await veterinarianRepository.GetPendingPagedAsync((page - 1) * pageSize, pageSize, cancellationToken);
        return Result.Success(veterinarians.Select(ClinicVeterinarianDto.FromDomain).ToList() as IReadOnlyList<ClinicVeterinarianDto>);
    }
}

file static class VerificationFilePolicy
{
    public const string ContainerName = "verification-documents";
    public const int MaxDocumentBytes = 5_242_880;
    public const int MaxSignatureBytes = 2_097_152;

    private static readonly HashSet<string> DocumentContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    private static readonly HashSet<string> SignatureContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public static bool IsAllowedDocument(string contentType, int length) =>
        length is > 0 and <= MaxDocumentBytes && DocumentContentTypes.Contains(contentType);

    public static bool IsAllowedSignature(string contentType, int length) =>
        length is > 0 and <= MaxSignatureBytes && SignatureContentTypes.Contains(contentType);

    public static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "application/pdf" => "pdf",
        "image/png" => "png",
        "image/webp" => "webp",
        _ => "jpg",
    };
}

public sealed record SubmitClinicVerificationCommand(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<ClinicVerificationDto>>;

public sealed class SubmitClinicVerificationCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVerificationRepository verificationRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SubmitClinicVerificationCommand, Result<ClinicVerificationDto>>
{
    public async Task<Result<ClinicVerificationDto>> Handle(
        SubmitClinicVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVerificationDto>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVerificationDto>("Acceso denegado.");

        var latest = await verificationRepository.GetLatestForClinicAsync(request.ClinicId, cancellationToken);
        if (latest is not null && latest.Status is ClinicVerificationStatus.Pending or ClinicVerificationStatus.Verified)
            return Result.Success(ClinicVerificationDto.FromDomain(latest));

        latest?.Supersede();
        if (latest is not null) verificationRepository.Update(latest);

        var verification = ClinicVerification.Submit(request.ClinicId, clinic.LicenseNumber, request.RequestingUserId);
        await verificationRepository.AddAsync(verification, cancellationToken);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVerification", verification.Id, VerificationAuditAction.ClinicVerificationSubmitted, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVerificationDto.FromDomain(verification));
    }
}

public sealed record UploadClinicVerificationDocumentCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    byte[] DocumentBytes,
    string ContentType) : IRequest<Result<ClinicVerificationDto>>;

public sealed class UploadClinicVerificationDocumentCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVerificationRepository verificationRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadClinicVerificationDocumentCommand, Result<ClinicVerificationDto>>
{
    public async Task<Result<ClinicVerificationDto>> Handle(
        UploadClinicVerificationDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!VerificationFilePolicy.IsAllowedDocument(request.ContentType, request.DocumentBytes.Length))
            return Result.Failure<ClinicVerificationDto>("Documento inválido. Solo PDF, JPEG, PNG o WebP de hasta 5 MB.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVerificationDto>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVerificationDto>("Acceso denegado.");

        var verification = await verificationRepository.GetLatestForClinicAsync(request.ClinicId, cancellationToken);
        if (verification is null || verification.Status is ClinicVerificationStatus.Rejected or ClinicVerificationStatus.Expired)
        {
            verification?.Supersede();
            if (verification is not null) verificationRepository.Update(verification);
            verification = ClinicVerification.Submit(request.ClinicId, clinic.LicenseNumber, request.RequestingUserId);
            await verificationRepository.AddAsync(verification, cancellationToken);
        }

        var extension = VerificationFilePolicy.ExtensionFor(request.ContentType);
        var blobName = $"clinics/{request.ClinicId}/verification/{verification.Id}/document.{extension}";
        using var stream = new MemoryStream(request.DocumentBytes);
        var url = await blobStorage.UploadAsync(VerificationFilePolicy.ContainerName, blobName, stream, request.ContentType, cancellationToken);

        verification.AttachDocument(url);
        verificationRepository.Update(verification);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVerification", verification.Id, VerificationAuditAction.ClinicVerificationDocumentUploaded, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVerificationDto.FromDomain(verification));
    }
}

public sealed record GetMyClinicVerificationQuery(Guid ClinicId, Guid RequestingUserId)
    : IRequest<Result<ClinicVerificationDto?>>;

public sealed class GetMyClinicVerificationQueryHandler(
    IClinicRepository clinicRepository,
    IClinicVerificationRepository verificationRepository)
    : IRequestHandler<GetMyClinicVerificationQuery, Result<ClinicVerificationDto?>>
{
    public async Task<Result<ClinicVerificationDto?>> Handle(
        GetMyClinicVerificationQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVerificationDto?>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVerificationDto?>("Acceso denegado.");

        var verification = await verificationRepository.GetLatestForClinicAsync(request.ClinicId, cancellationToken);
        return Result.Success<ClinicVerificationDto?>(verification is null ? null : ClinicVerificationDto.FromDomain(verification));
    }
}

public sealed record ReviewClinicVerificationCommand(
    Guid VerificationId,
    Guid AdminUserId,
    bool Approve,
    DateOnly? ExpiresAt,
    string? Reason,
    string? Notes) : IRequest<Result<ClinicVerificationDto>>;

public sealed class ReviewClinicVerificationCommandValidator : AbstractValidator<ReviewClinicVerificationCommand>
{
    public ReviewClinicVerificationCommandValidator()
    {
        RuleFor(command => command.VerificationId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
        RuleFor(command => command.ExpiresAt)
            .NotNull().When(command => command.Approve)
            .WithMessage("La fecha de vencimiento es requerida al aprobar.");
        RuleFor(command => command.Reason)
            .NotEmpty().When(command => !command.Approve)
            .WithMessage("El motivo de rechazo es requerido.");
    }
}

public sealed class ReviewClinicVerificationCommandHandler(
    IClinicVerificationRepository verificationRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewClinicVerificationCommand, Result<ClinicVerificationDto>>
{
    public async Task<Result<ClinicVerificationDto>> Handle(
        ReviewClinicVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var verification = await verificationRepository.GetByIdAsync(request.VerificationId, cancellationToken);
        if (verification is null) return Result.Failure<ClinicVerificationDto>("Verificación no encontrada.");

        var result = request.Approve
            ? verification.Verify(request.AdminUserId, request.ExpiresAt, request.Notes)
            : verification.Reject(request.AdminUserId, request.Reason!);
        if (result.IsFailure) return Result.Failure<ClinicVerificationDto>(result.Errors);

        verificationRepository.Update(verification);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVerification",
            verification.Id,
            request.Approve ? VerificationAuditAction.ClinicVerificationApproved : VerificationAuditAction.ClinicVerificationRejected,
            request.AdminUserId,
            request.Approve ? request.Notes : request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVerificationDto.FromDomain(verification));
    }
}

public sealed record DownloadClinicVerificationDocumentQuery(Guid VerificationId, Guid RequestingUserId, bool IsAdmin)
    : IRequest<Result<VerificationDocumentDownloadDto>>;

public sealed class DownloadClinicVerificationDocumentQueryHandler(
    IClinicVerificationRepository verificationRepository,
    IClinicRepository clinicRepository,
    IBlobStorageService blobStorage,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadClinicVerificationDocumentQuery, Result<VerificationDocumentDownloadDto>>
{
    public async Task<Result<VerificationDocumentDownloadDto>> Handle(
        DownloadClinicVerificationDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var verification = await verificationRepository.GetByIdAsync(request.VerificationId, cancellationToken);
        if (verification is null) return Result.Failure<VerificationDocumentDownloadDto>("Verificación no encontrada.");
        if (string.IsNullOrWhiteSpace(verification.DocumentUrl))
            return Result.Failure<VerificationDocumentDownloadDto>("Documento no disponible.");

        if (!request.IsAdmin)
        {
            var clinic = await clinicRepository.GetByIdAsync(verification.ClinicId, cancellationToken);
            if (clinic is null || clinic.UserId != request.RequestingUserId)
                return Result.Failure<VerificationDocumentDownloadDto>("Acceso denegado.");
        }

        var bytes = await blobStorage.DownloadAsync(verification.DocumentUrl, cancellationToken);
        if (bytes is null) return Result.Failure<VerificationDocumentDownloadDto>("Documento no disponible.");

        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVerification", verification.Id, VerificationAuditAction.DocumentDownloaded, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new VerificationDocumentDownloadDto(bytes, $"clinic-verification-{verification.Id}.pdf", "application/octet-stream"));
    }
}

public sealed record VerifyClinicForCertificatesCommand(
    Guid ClinicId,
    Guid AdminUserId,
    DateOnly? ExpiresAt) : IRequest<Result<ClinicVerificationDto>>;

public sealed class VerifyClinicForCertificatesCommandValidator
    : AbstractValidator<VerifyClinicForCertificatesCommand>
{
    public VerifyClinicForCertificatesCommandValidator()
    {
        RuleFor(command => command.ClinicId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
    }
}

public sealed class VerifyClinicForCertificatesCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVerificationRepository verificationRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<VerifyClinicForCertificatesCommand, Result<ClinicVerificationDto>>
{
    public async Task<Result<ClinicVerificationDto>> Handle(
        VerifyClinicForCertificatesCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicVerificationDto>("Clínica no encontrada.");

        var verification = ClinicVerification.Submit(clinic.Id, clinic.LicenseNumber);
        verification.AttachDocument($"manual://admin-verification/{clinic.Id:N}");
        var verifyResult = verification.Verify(request.AdminUserId, request.ExpiresAt, "Verificación manual out-of-band por administración.");
        if (verifyResult.IsFailure)
            return Result.Failure<ClinicVerificationDto>(verifyResult.Errors);

        await verificationRepository.AddAsync(verification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVerificationDto.FromDomain(verification));
    }
}

public sealed record CreateClinicVeterinarianCommand(
    Guid ClinicId,
    Guid RequestingUserId,
    string FullName,
    string LicenseNumber) : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class CreateClinicVeterinarianCommandValidator
    : AbstractValidator<CreateClinicVeterinarianCommand>
{
    public CreateClinicVeterinarianCommandValidator()
    {
        RuleFor(command => command.ClinicId).NotEmpty();
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.LicenseNumber).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateClinicVeterinarianCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateClinicVeterinarianCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        CreateClinicVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<ClinicVeterinarianDto>("Clínica no encontrada.");

        if (clinic.UserId != request.RequestingUserId)
            return Result.Failure<ClinicVeterinarianDto>("Acceso denegado.");

        if (await veterinarianRepository.LicenseExistsForClinicAsync(request.ClinicId, request.LicenseNumber, cancellationToken))
            return Result.Failure<ClinicVeterinarianDto>("Ya existe un veterinario con esa licencia en la clínica.");

        var veterinarian = ClinicVeterinarian.Submit(request.ClinicId, request.RequestingUserId, request.FullName, request.LicenseNumber);
        await veterinarianRepository.AddAsync(veterinarian, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record UploadVeterinarianDocumentCommand(
    Guid ClinicId,
    Guid VeterinarianId,
    Guid RequestingUserId,
    byte[] DocumentBytes,
    string ContentType) : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class UploadVeterinarianDocumentCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadVeterinarianDocumentCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        UploadVeterinarianDocumentCommand request,
        CancellationToken cancellationToken)
    {
        if (!VerificationFilePolicy.IsAllowedDocument(request.ContentType, request.DocumentBytes.Length))
            return Result.Failure<ClinicVeterinarianDto>("Documento inválido. Solo PDF, JPEG, PNG o WebP de hasta 5 MB.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVeterinarianDto>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVeterinarianDto>("Acceso denegado.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId)
            return Result.Failure<ClinicVeterinarianDto>("Veterinario no encontrado.");

        var extension = VerificationFilePolicy.ExtensionFor(request.ContentType);
        var blobName = $"clinics/{request.ClinicId}/veterinarians/{request.VeterinarianId}/document.{extension}";
        using var stream = new MemoryStream(request.DocumentBytes);
        var url = await blobStorage.UploadAsync(VerificationFilePolicy.ContainerName, blobName, stream, request.ContentType, cancellationToken);

        veterinarian.AttachDocument(url);
        veterinarianRepository.Update(veterinarian);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.VeterinarianDocumentUploaded, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record UploadVeterinarianSignatureCommand(
    Guid ClinicId,
    Guid VeterinarianId,
    Guid RequestingUserId,
    byte[] SignatureBytes,
    string ContentType) : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class UploadVeterinarianSignatureCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadVeterinarianSignatureCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        UploadVeterinarianSignatureCommand request,
        CancellationToken cancellationToken)
    {
        if (!VerificationFilePolicy.IsAllowedSignature(request.ContentType, request.SignatureBytes.Length))
            return Result.Failure<ClinicVeterinarianDto>("Firma inválida. Solo JPEG, PNG o WebP de hasta 2 MB.");

        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVeterinarianDto>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVeterinarianDto>("Acceso denegado.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId)
            return Result.Failure<ClinicVeterinarianDto>("Veterinario no encontrado.");

        var extension = VerificationFilePolicy.ExtensionFor(request.ContentType);
        var blobName = $"clinics/{request.ClinicId}/veterinarians/{request.VeterinarianId}/signature.{extension}";
        using var stream = new MemoryStream(request.SignatureBytes);
        var url = await blobStorage.UploadAsync(VerificationFilePolicy.ContainerName, blobName, stream, request.ContentType, cancellationToken);

        veterinarian.AttachSignature(url);
        veterinarianRepository.Update(veterinarian);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.VeterinarianSignatureUploaded, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record ReviewClinicVeterinarianCommand(
    Guid VeterinarianId,
    Guid AdminUserId,
    bool Approve,
    DateOnly? ExpiresAt,
    string? Reason,
    string? Notes) : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class ReviewClinicVeterinarianCommandHandler(
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReviewClinicVeterinarianCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        ReviewClinicVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null) return Result.Failure<ClinicVeterinarianDto>("Veterinario no encontrado.");

        var result = request.Approve
            ? veterinarian.Authorize(request.AdminUserId, request.ExpiresAt, request.Notes)
            : veterinarian.Reject(request.AdminUserId, request.Reason!);
        if (result.IsFailure) return Result.Failure<ClinicVeterinarianDto>(result.Errors);

        veterinarianRepository.Update(veterinarian);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian",
            veterinarian.Id,
            request.Approve ? VerificationAuditAction.VeterinarianAuthorized : VerificationAuditAction.VeterinarianRejected,
            request.AdminUserId,
            request.Approve ? request.Notes : request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record SuspendClinicVeterinarianCommand(Guid VeterinarianId, Guid AdminUserId, string Reason)
    : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class SuspendClinicVeterinarianCommandHandler(
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SuspendClinicVeterinarianCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        SuspendClinicVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null) return Result.Failure<ClinicVeterinarianDto>("Veterinario no encontrado.");

        var result = veterinarian.Suspend(request.AdminUserId, request.Reason);
        if (result.IsFailure) return Result.Failure<ClinicVeterinarianDto>(result.Errors);

        veterinarianRepository.Update(veterinarian);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.VeterinarianSuspended, request.AdminUserId, request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record RevokeMyClinicVeterinarianCommand(Guid ClinicId, Guid VeterinarianId, Guid RequestingUserId, string Reason)
    : IRequest<Result<ClinicVeterinarianDto>>;

public sealed class RevokeMyClinicVeterinarianCommandHandler(
    IClinicRepository clinicRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RevokeMyClinicVeterinarianCommand, Result<ClinicVeterinarianDto>>
{
    public async Task<Result<ClinicVeterinarianDto>> Handle(
        RevokeMyClinicVeterinarianCommand request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null) return Result.Failure<ClinicVeterinarianDto>("Clínica no encontrada.");
        if (clinic.UserId != request.RequestingUserId) return Result.Failure<ClinicVeterinarianDto>("Acceso denegado.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId)
            return Result.Failure<ClinicVeterinarianDto>("Veterinario no encontrado.");

        var result = veterinarian.Revoke(request.RequestingUserId, request.Reason);
        if (result.IsFailure) return Result.Failure<ClinicVeterinarianDto>(result.Errors);

        veterinarianRepository.Update(veterinarian);
        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.VeterinarianRevoked, request.RequestingUserId, request.Reason), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ClinicVeterinarianDto.FromDomain(veterinarian));
    }
}

public sealed record DownloadVeterinarianDocumentQuery(Guid VeterinarianId, Guid RequestingUserId, bool IsAdmin)
    : IRequest<Result<VerificationDocumentDownloadDto>>;

public sealed class DownloadVeterinarianDocumentQueryHandler(
    IClinicVeterinarianRepository veterinarianRepository,
    IClinicRepository clinicRepository,
    IBlobStorageService blobStorage,
    IVerificationAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadVeterinarianDocumentQuery, Result<VerificationDocumentDownloadDto>>
{
    public async Task<Result<VerificationDocumentDownloadDto>> Handle(
        DownloadVeterinarianDocumentQuery request,
        CancellationToken cancellationToken)
    {
        var veterinarian = await veterinarianRepository.GetByIdAsync(request.VeterinarianId, cancellationToken);
        if (veterinarian is null) return Result.Failure<VerificationDocumentDownloadDto>("Veterinario no encontrado.");
        if (string.IsNullOrWhiteSpace(veterinarian.DocumentUrl))
            return Result.Failure<VerificationDocumentDownloadDto>("Documento no disponible.");

        if (!request.IsAdmin)
        {
            var clinic = await clinicRepository.GetByIdAsync(veterinarian.ClinicId, cancellationToken);
            if (clinic is null || clinic.UserId != request.RequestingUserId)
                return Result.Failure<VerificationDocumentDownloadDto>("Acceso denegado.");
        }

        var bytes = await blobStorage.DownloadAsync(veterinarian.DocumentUrl, cancellationToken);
        if (bytes is null) return Result.Failure<VerificationDocumentDownloadDto>("Documento no disponible.");

        await auditLogRepository.AddAsync(VerificationAuditLog.Create(
            "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.DocumentDownloaded, request.RequestingUserId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new VerificationDocumentDownloadDto(bytes, $"veterinarian-{veterinarian.Id}.pdf", "application/octet-stream"));
    }
}
