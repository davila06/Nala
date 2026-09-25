using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.UploadClinicalConsultationAttachment;

public sealed record UploadClinicalConsultationAttachmentCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid ConsultationId,
    byte[] FileBytes,
    string ContentType)
    : IRequest<Result<string>>;

public sealed class UploadClinicalConsultationAttachmentCommandValidator
    : AbstractValidator<UploadClinicalConsultationAttachmentCommand>
{
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public UploadClinicalConsultationAttachmentCommandValidator()
    {
        RuleFor(x => x.ConsultationId).NotEmpty();
        RuleFor(x => x.FileBytes).NotEmpty().Must(bytes => bytes.Length <= 5 * 1024 * 1024)
            .WithMessage("El adjunto no puede superar 5 MB.");
        RuleFor(x => x.ContentType).Must(value => AllowedContentTypes.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Solo se aceptan PDF, JPEG o PNG.");
    }
}

public sealed class UploadClinicalConsultationAttachmentCommandHandler(
    IClinicalConsultationRepository consultationRepository,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UploadClinicalConsultationAttachmentCommand, Result<string>>
{
    private const string Container = "clinical-consultation-attachments";

    public async Task<Result<string>> Handle(UploadClinicalConsultationAttachmentCommand request, CancellationToken cancellationToken)
    {
        var consultation = await consultationRepository.GetByIdAsync(request.ConsultationId, cancellationToken);
        if (consultation is null || consultation.ClinicId != request.ClinicId)
            return Result.Failure<string>("Consulta no encontrada.");

        var extension = request.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? "pdf"
            : request.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";
        var blobName = $"{consultation.ClinicId}/{consultation.Id}/{Guid.CreateVersion7():N}.{extension}";
        await using var stream = new MemoryStream(request.FileBytes);
        var url = await blobStorage.UploadAsync(Container, blobName, stream, request.ContentType, cancellationToken);

        try
        {
            consultation.SetAttachmentUrl(url);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<string>(ex.Message);
        }

        consultationRepository.Update(consultation);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.ClinicUserId,
                AuditAction.ClinicalConsultationAttachmentUploaded,
                "ClinicalConsultation",
                consultation.Id.ToString(),
                request.ContentType),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(url);
    }
}
