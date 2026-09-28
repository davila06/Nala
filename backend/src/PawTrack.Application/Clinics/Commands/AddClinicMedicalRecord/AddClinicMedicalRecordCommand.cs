using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Clinics.Commands.AddClinicMedicalRecord;

// ── Command ───────────────────────────────────────────────────────────────────

/// <summary>
/// Allows an authenticated clinic to add a medical record to a pet's expediente.
/// A QR/RFID scan identifies the pet but does not grant access to its medical record.
/// The clinic must have an active owner-approved grant with the Write permission.
/// </summary>
public sealed record AddClinicMedicalRecordCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    // ── Access resolution ────────────────────────────────────────────────────
    Guid? PetId,           // Option A: known petId from a previous scan result
    string? QrOrChipInput, // Option B: raw QR URL or RFID chip from the current consult
    ScanInputType? InputType,
    // ── Record payload ───────────────────────────────────────────────────────
    MedicalRecordType RecordType,
    DateOnly Date,
    string Description,
    string? VetName,
    DateOnly? NextDueDate,
    byte[]? DocumentBytes,
    string? DocumentContentType,
    MedicalDocumentKind? DocumentKind = null)
    : IRequest<Result<MedicalRecordDto>>;

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class AddClinicMedicalRecordCommandValidator
    : AbstractValidator<AddClinicMedicalRecordCommand>
{
    public AddClinicMedicalRecordCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.PetId.HasValue || !string.IsNullOrWhiteSpace(x.QrOrChipInput))
            .WithMessage("Se requiere PetId (opción A) o QrOrChipInput (opción B).");

        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.VetName).MaximumLength(120);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha no puede ser futura.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class AddClinicMedicalRecordCommandHandler(
    IClinicRepository clinicRepository,
    IClinicScanRepository clinicScanRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    IMedicalRepository medicalRepository,
    INotificationDispatcher notificationDispatcher,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddClinicMedicalRecordCommand, Result<MedicalRecordDto>>
{
    private const string MedicalDocsContainer = "medical-docs";
    private static readonly Regex PetIdFromQrPattern =
        new(@"\/p\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<Result<MedicalRecordDto>> Handle(
        AddClinicMedicalRecordCommand request, CancellationToken ct)
    {
        if (request.DocumentKind.HasValue && request.DocumentBytes is not { Length: > 0 })
            return Result.Failure<MedicalRecordDto>("El tipo de documento requiere un adjunto.");
        // Verify clinic is active
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.Status != ClinicStatus.Active || clinic.UserId != request.ClinicUserId)
            return Result.Failure<MedicalRecordDto>("La clínica no está activa.");

        // ── Resolve pet + create inline scan if needed (Option B) ────────────
        var (pet, inlineScan) = await ResolvePetAsync(request, ct);
        if (pet is null)
            return Result.Failure<MedicalRecordDto>(
                "No se pudo identificar la mascota. Verifique el QR o chip.");

        // A scan identifies the patient; only explicit owner consent authorizes a write.
        var grant = await grantRepository.GetActiveGrantAsync(request.ClinicId, pet.Id, ct);
        if (grant is null || !grant.HasPermission(ClinicMedicalAccessPermission.Write))
            return Result.Failure<MedicalRecordDto>(
                "La clínica no tiene acceso de escritura al expediente; requiere consentimiento activo del dueño.");

        // ── Create medical record ─────────────────────────────────────────────
        var record = MedicalRecord.Create(
            pet.Id, request.ClinicUserId, request.RecordType,
            request.Date, request.Description,
            request.VetName, clinic.Name, request.NextDueDate,
            clinicId: request.ClinicId);

        if (request.DocumentBytes is { Length: > 0 } && !string.IsNullOrEmpty(request.DocumentContentType))
        {
            var ext = request.DocumentContentType.Contains("pdf") ? "pdf"
                    : request.DocumentContentType.Contains("png") ? "png" : "jpg";
            var blobName = $"{pet.Id}/{record.Id}.{ext}";
            using var stream = new MemoryStream(request.DocumentBytes);
            var url = await blobStorage.UploadAsync(MedicalDocsContainer, blobName, stream, request.DocumentContentType, ct);
            record.SetDocumentUrl(url, request.DocumentKind, request.DocumentContentType);
        }

        await medicalRepository.AddAsync(record, ct);

        // Auto-reminder if NextDueDate provided
        if (request.NextDueDate.HasValue)
        {
            var reminder = VetReminder.Create(
                pet.Id, request.ClinicUserId, request.RecordType,
                request.NextDueDate.Value,
                $"{request.RecordType} — {pet.Name}",
                $"Programado por {clinic.Name} el {request.Date:dd/MM/yyyy}");
            await medicalRepository.AddReminderAsync(reminder, ct);
        }

        // Persist inline scan if option B was used
        if (inlineScan is not null)
            await clinicScanRepository.AddAsync(inlineScan, ct);

        await unitOfWork.SaveChangesAsync(ct);

        // Notify owner (fire-and-forget — failure must not roll back the record)
        var owner = await userRepository.GetByIdAsync(pet.OwnerId, ct);
        if (owner is not null)
        {
            _ = notificationDispatcher.DispatchClinicMedicalRecordAddedAsync(
                owner.Id, pet.Name, clinic.Name, record.Type.ToString(), ct);
        }

        return Result.Success(MedicalRecordDto.FromDomain(record));
    }

    private async Task<(Domain.Pets.Pet? pet, ClinicScan? inlineScan)> ResolvePetAsync(
        AddClinicMedicalRecordCommand request, CancellationToken ct)
    {
        // QR/chip identifies the consulted pet and must override a client-supplied ID.
        if (!string.IsNullOrWhiteSpace(request.QrOrChipInput))
        {
            Domain.Pets.Pet? resolvedPet = null;
            var inputType = request.InputType ?? ScanInputType.Qr;

            if (inputType == ScanInputType.Qr)
            {
                var match = PetIdFromQrPattern.Match(request.QrOrChipInput);
                if (match.Success && Guid.TryParse(match.Groups[1].Value, out var petId))
                    resolvedPet = await petRepository.GetByIdAsync(petId, ct);
            }
            else if (inputType == ScanInputType.RfidChip)
            {
                resolvedPet = await petRepository.GetByMicrochipIdAsync(
                    request.QrOrChipInput.Trim().ToUpperInvariant(), ct);
            }

            return resolvedPet is null
                ? (null, null)
                : (resolvedPet, ClinicScan.Create(request.ClinicId, request.QrOrChipInput, inputType, resolvedPet.Id));
        }

        return request.PetId.HasValue
            ? (await petRepository.GetByIdAsync(request.PetId.Value, ct), null)
            : (null, null);
    }
}
