using FluentValidation;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical;

// ── Shared family access helper ───────────────────────────────────────────────

file static class FamilyAccessChecker
{
    internal static async Task<bool> CanAccessPetAsync(
        Guid petOwnerId, Guid userId,
        IFamilyRepository familyRepository,
        CancellationToken ct)
    {
        if (petOwnerId == userId) return true;
        var memberIds = await familyRepository.GetActiveMemberIdsAsync(petOwnerId, ct);
        return memberIds.Contains(userId);
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record MedicalRecordDto(
    Guid Id,
    Guid PetId,
    string Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate,
    string? DocumentUrl,
    DateTimeOffset CreatedAt,
    Guid? ClinicId,
    string Source,
    decimal? WeightKg,
    string? DosageDescription,
    string? Frequency,
    int? DurationDays,
    DateOnly? MedicationEndDate)
{
    public static MedicalRecordDto FromDomain(MedicalRecord r) => new(
        r.Id, r.PetId, r.Type.ToString(), r.Date,
        r.Description, r.VetName, r.ClinicName,
        r.NextDueDate, r.DocumentUrl, r.CreatedAt,
        r.ClinicId,
        r.ClinicId.HasValue ? "Clinic" : "Owner",
        r.WeightKg, r.DosageDescription, r.Frequency, r.DurationDays, r.MedicationEndDate);
}

public sealed record VetReminderDto(
    Guid Id,
    Guid PetId,
    string Type,
    DateOnly DueDate,
    string Title,
    string? Notes,
    bool IsCompleted)
{
    public static VetReminderDto FromDomain(VetReminder r) =>
        new(r.Id, r.PetId, r.Type.ToString(), r.DueDate, r.Title, r.Notes, r.IsCompleted);
}

// ── Add medical record ────────────────────────────────────────────────────────

public sealed record AddMedicalRecordCommand(
    Guid PetId,
    Guid RequestingUserId,
    MedicalRecordType Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate,
    byte[]? DocumentBytes,
    string? DocumentContentType,
    decimal? WeightKg = null,
    string? DosageDescription = null,
    string? Frequency = null,
    int? DurationDays = null,
    DateOnly? MedicationEndDate = null) : IRequest<Result<MedicalRecordDto>>;

public sealed class AddMedicalRecordCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddMedicalRecordCommand, Result<MedicalRecordDto>>
{
    private const string MedicalDocsContainer = "medical-docs";

    public async Task<Result<MedicalRecordDto>> Handle(
        AddMedicalRecordCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<MedicalRecordDto>("El historial médico requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<MedicalRecordDto>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<MedicalRecordDto>("Acceso denegado.");

        var record = MedicalRecord.Create(
            request.PetId, request.RequestingUserId, request.Type,
            request.Date, request.Description,
            request.VetName, request.ClinicName, request.NextDueDate,
            weightKg: request.WeightKg,
            dosageDescription: request.DosageDescription,
            frequency: request.Frequency,
            durationDays: request.DurationDays,
            medicationEndDate: request.MedicationEndDate);

        if (request.DocumentBytes is { Length: > 0 })
        {
            var ext = request.DocumentContentType == "application/pdf" ? "pdf" : "jpg";
            var blobName = $"{request.PetId}/{record.Id}.{ext}";
            using var stream = new MemoryStream(request.DocumentBytes);
            var url = await blobStorage.UploadAsync(
                MedicalDocsContainer, blobName, stream, request.DocumentContentType!, ct);
            record.SetDocumentUrl(url);
        }

        await medicalRepository.AddAsync(record, ct);

        // Auto-create reminder if NextDueDate is set
        if (request.NextDueDate.HasValue)
        {
            var reminder = VetReminder.Create(
                request.PetId, request.RequestingUserId, request.Type,
                request.NextDueDate.Value,
                $"{request.Type} — {pet.Name}",
                $"Originado desde historial médico del {request.Date:dd/MM/yyyy}");
            await medicalRepository.AddReminderAsync(reminder, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MedicalRecordDto.FromDomain(record));
    }
}

// ── Get medical history ───────────────────────────────────────────────────────

// ── Get record count (no plan gate — used for upgrade teaser) ─────────────────

public sealed record MedicalRecordCountDto(int TotalRecords, int ClinicRecords);

/// <summary>
/// Tiered response: Familia → full history; Plus → 3-record preview with masked fields;
/// Explorador → empty list (frontend falls back to count-teaser endpoint).
/// </summary>
public sealed record MedicalHistoryResultDto(
    IReadOnlyList<MedicalRecordDto> Records,
    int TotalCount,
    /// <summary>"familia" | "plus_preview" | "explorador"</summary>
    string AccessTier,
    bool IsLimited,
    int? PreviewLimit);

public sealed record GetMedicalRecordCountQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<MedicalRecordCountDto>>;

public sealed class GetMedicalRecordCountQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IMedicalRepository medicalRepository)
    : IRequestHandler<GetMedicalRecordCountQuery, Result<MedicalRecordCountDto>>
{
    public async Task<Result<MedicalRecordCountDto>> Handle(
        GetMedicalRecordCountQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<MedicalRecordCountDto>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<MedicalRecordCountDto>("Acceso denegado.");

        var records = await medicalRepository.GetByPetIdAsync(request.PetId, ct);
        var clinicCount = records.Count(r => r.ClinicId.HasValue);
        return Result.Success(new MedicalRecordCountDto(records.Count, clinicCount));
    }
}
public sealed record GetMedicalHistoryQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<MedicalHistoryResultDto>>;

public sealed class GetMedicalHistoryQueryHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetMedicalHistoryQuery, Result<MedicalHistoryResultDto>>
{
    private const int PlusPreviewLimit = 3;

    public async Task<Result<MedicalHistoryResultDto>> Handle(
        GetMedicalHistoryQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<MedicalHistoryResultDto>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<MedicalHistoryResultDto>("Acceso denegado.");

        var tier = await subscriptionService.GetActiveUserTierAsync(request.RequestingUserId, ct);
        var records = await medicalRepository.GetByPetIdAsync(request.PetId, ct);
        var total = records.Count;

        // Familia: full access
        if (tier == PawTrack.Domain.Subscriptions.SubscriptionTier.UserFamilia)
        {
            return Result.Success(new MedicalHistoryResultDto(
                records.Select(MedicalRecordDto.FromDomain).ToList(),
                total, "familia", false, null));
        }

        // Plus: preview — last 3 records, sensitive fields masked
        if (tier == PawTrack.Domain.Subscriptions.SubscriptionTier.UserPlus)
        {
            var preview = records
                .Take(PlusPreviewLimit)
                .Select(r => MedicalRecordDto.FromDomain(r) with
                {
                    DocumentUrl = null,       // documents are Familia-only
                    WeightKg = null,          // health metrics are Familia-only
                    DosageDescription = null,
                    Frequency = null,
                    DurationDays = null,
                    MedicationEndDate = null,
                })
                .ToList();
            return Result.Success(new MedicalHistoryResultDto(
                preview, total, "plus_preview", true, PlusPreviewLimit));
        }

        // Explorador: return empty — frontend shows count teaser from /medical/count
        return Result.Success(new MedicalHistoryResultDto(
            [], total, "explorador", true, 0));
    }
}

// ── Get vet reminders ─────────────────────────────────────────────────────────

public sealed record GetVetRemindersQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<VetReminderDto>>>;

public sealed class GetVetRemindersQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IMedicalRepository medicalRepository)
    : IRequestHandler<GetVetRemindersQuery, Result<IReadOnlyList<VetReminderDto>>>
{
    public async Task<Result<IReadOnlyList<VetReminderDto>>> Handle(
        GetVetRemindersQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<IReadOnlyList<VetReminderDto>>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<IReadOnlyList<VetReminderDto>>("Acceso denegado.");

        var reminders = await medicalRepository.GetUpcomingRemindersAsync(request.PetId, ct);
        return Result.Success<IReadOnlyList<VetReminderDto>>(
            reminders.Select(VetReminderDto.FromDomain).ToList());
    }
}

// ── Mark reminder completed ───────────────────────────────────────────────────

public sealed record CompleteVetReminderCommand(Guid ReminderId, Guid RequestingUserId) : IRequest<Result<bool>>;

public sealed class CompleteVetReminderCommandHandler(
    IMedicalRepository medicalRepository,
    IPetRepository petRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CompleteVetReminderCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CompleteVetReminderCommand request, CancellationToken ct)
    {
        var reminder = await medicalRepository.GetReminderByIdAsync(request.ReminderId, ct);
        if (reminder is null) return Result.Failure<bool>("Recordatorio no encontrado.");

        var pet = await petRepository.GetByIdAsync(reminder.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId)
            return Result.Failure<bool>("Acceso denegado.");

        reminder.MarkCompleted();
        medicalRepository.UpdateReminder(reminder);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(true);
    }
}

// ── Delete medical record ─────────────────────────────────────────────────────

public sealed record DeleteMedicalRecordCommand(Guid RecordId, Guid RequestingUserId)
    : IRequest<Result<Unit>>;

public sealed class DeleteMedicalRecordCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteMedicalRecordCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteMedicalRecordCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<Unit>("El historial médico requiere el plan Familia.");

        var record = await medicalRepository.GetByIdAsync(request.RecordId, ct);
        if (record is null) return Result.Failure<Unit>("Registro no encontrado.");

        var pet = await petRepository.GetByIdAsync(record.PetId, ct);
        if (pet is null) return Result.Failure<Unit>("Mascota no encontrada.");

        // Authorize: creator, pet owner, or family member of owner
        var canDelete = record.CreatedByUserId == request.RequestingUserId
            || pet.OwnerId == request.RequestingUserId
            || await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct);

        if (!canDelete)
            return Result.Failure<Unit>("Solo el creador del registro o el dueño de la mascota puede eliminarlo.");

        // Best-effort blob cleanup — do not block deletion on storage failure
        if (!string.IsNullOrEmpty(record.DocumentUrl))
        {
            try { await blobStorage.DeleteAsync(record.DocumentUrl, ct); }
            catch { /* intentional: storage cleanup is non-critical */ }
        }

        medicalRepository.Delete(record);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Update medical record ─────────────────────────────────────────────────────

public sealed record UpdateMedicalRecordCommand(
    Guid RecordId,
    Guid RequestingUserId,
    MedicalRecordType Type,
    DateOnly Date,
    string Description,
    string? VetName,
    string? ClinicName,
    DateOnly? NextDueDate,
    decimal? WeightKg = null,
    string? DosageDescription = null,
    string? Frequency = null,
    int? DurationDays = null,
    DateOnly? MedicationEndDate = null) : IRequest<Result<MedicalRecordDto>>;

public sealed class UpdateMedicalRecordCommandValidator : AbstractValidator<UpdateMedicalRecordCommand>
{
    public UpdateMedicalRecordCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.VetName).MaximumLength(200).When(x => x.VetName is not null);
        RuleFor(x => x.ClinicName).MaximumLength(200).When(x => x.ClinicName is not null);
        RuleFor(x => x.Date).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)));
    }
}

public sealed class UpdateMedicalRecordCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateMedicalRecordCommand, Result<MedicalRecordDto>>
{
    public async Task<Result<MedicalRecordDto>> Handle(UpdateMedicalRecordCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<MedicalRecordDto>("El historial médico requiere el plan Familia.");

        var record = await medicalRepository.GetByIdAsync(request.RecordId, ct);
        if (record is null) return Result.Failure<MedicalRecordDto>("Registro no encontrado.");

        var pet = await petRepository.GetByIdAsync(record.PetId, ct);
        if (pet is null) return Result.Failure<MedicalRecordDto>("Mascota no encontrada.");

        // Only the creator or family members can edit content
        var canEdit = record.CreatedByUserId == request.RequestingUserId
            || await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct);

        if (!canEdit)
            return Result.Failure<MedicalRecordDto>("Solo el creador del registro puede editarlo.");

        record.Update(request.Type, request.Date, request.Description,
            request.VetName, request.ClinicName, request.NextDueDate,
            request.WeightKg, request.DosageDescription,
            request.Frequency, request.DurationDays, request.MedicationEndDate);
        medicalRepository.Update(record);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(MedicalRecordDto.FromDomain(record));
    }
}

// ── Create standalone vet reminder ────────────────────────────────────────────

public sealed record CreateVetReminderCommand(
    Guid PetId,
    Guid RequestingUserId,
    MedicalRecordType Type,
    DateOnly DueDate,
    string Title,
    string? Notes) : IRequest<Result<VetReminderDto>>;

public sealed class CreateVetReminderCommandValidator : AbstractValidator<CreateVetReminderCommand>
{
    public CreateVetReminderCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.DueDate).GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.Date));
    }
}

// ── Get all reminders across all user's pets ──────────────────────────────────

public sealed record PetReminderDto(
    Guid ReminderId,
    Guid PetId,
    string PetName,
    string? PetPhotoUrl,
    string Type,
    DateOnly DueDate,
    string Title,
    string? Notes,
    bool IsCompleted,
    bool IsOverdue);

public sealed record GetMyRemindersQuery(Guid UserId, int DaysAhead = 30)
    : IRequest<Result<IReadOnlyList<PetReminderDto>>>;

public sealed class GetMyRemindersQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IMedicalRepository medicalRepository)
    : IRequestHandler<GetMyRemindersQuery, Result<IReadOnlyList<PetReminderDto>>>
{
    public async Task<Result<IReadOnlyList<PetReminderDto>>> Handle(
        GetMyRemindersQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(request.DaysAhead);

        // Collect all pets visible to this user
        var ownedPets = await petRepository.GetByOwnerIdAsync(request.UserId, ct);
        var allPets = ownedPets.ToList();

        // If user is a family member, include the family account owner's pets
        var familyAsOwner = await familyRepository.GetByOwnerAsync(request.UserId, ct);
        if (familyAsOwner is null)
        {
            var familyAsMember = await familyRepository.GetByMemberAsync(request.UserId, ct);
            if (familyAsMember is not null)
            {
                var ownerPets = await petRepository.GetByOwnerIdAsync(familyAsMember.OwnerId, ct);
                allPets.AddRange(ownerPets.Where(p => allPets.All(op => op.Id != p.Id)));
            }
        }

        var result = new List<PetReminderDto>();
        foreach (var pet in allPets)
        {
            var reminders = await medicalRepository.GetUpcomingRemindersAsync(pet.Id, ct);
            foreach (var r in reminders)
            {
                // Include overdue (past) + upcoming within window
                if (r.IsCompleted) continue;
                if (r.DueDate > cutoff) continue;

                result.Add(new PetReminderDto(
                    r.Id, pet.Id, pet.Name, pet.PhotoUrl,
                    r.Type.ToString(), r.DueDate, r.Title, r.Notes,
                    r.IsCompleted,
                    r.DueDate < today));
            }
        }

        return Result.Success<IReadOnlyList<PetReminderDto>>(
            result.OrderBy(r => r.DueDate).ToList());
    }
}

// ── Get clinic access log for a pet (owner only) ──────────────────────────────

public sealed record ClinicAccessLogEntryDto(
    Guid LogId,
    Guid ClinicId,
    string? ClinicName,
    DateTimeOffset AccessedAt);

public sealed record GetClinicAccessLogQuery(Guid PetId, Guid RequestingUserId, int Limit = 50)
    : IRequest<Result<IReadOnlyList<ClinicAccessLogEntryDto>>>;

public sealed class GetClinicAccessLogQueryHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IClinicMedicalAccessLogRepository accessLogRepository,
    IClinicRepository clinicRepository)
    : IRequestHandler<GetClinicAccessLogQuery, Result<IReadOnlyList<ClinicAccessLogEntryDto>>>
{
    public async Task<Result<IReadOnlyList<ClinicAccessLogEntryDto>>> Handle(
        GetClinicAccessLogQuery request, CancellationToken ct)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<IReadOnlyList<ClinicAccessLogEntryDto>>("Mascota no encontrada.");

        var canView = pet.OwnerId == request.RequestingUserId
            || await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct);
        if (!canView) return Result.Failure<IReadOnlyList<ClinicAccessLogEntryDto>>("Acceso denegado.");

        var logs = await accessLogRepository.GetByPetIdAsync(request.PetId, request.Limit, ct);

        var entries = new List<ClinicAccessLogEntryDto>();
        foreach (var log in logs)
        {
            var clinic = await clinicRepository.GetByIdAsync(log.ClinicId, ct);
            entries.Add(new ClinicAccessLogEntryDto(log.Id, log.ClinicId, clinic?.Name, log.AccessedAt));
        }

        return Result.Success<IReadOnlyList<ClinicAccessLogEntryDto>>(entries);
    }
}

public sealed class CreateVetReminderCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateVetReminderCommand, Result<VetReminderDto>>
{
    public async Task<Result<VetReminderDto>> Handle(CreateVetReminderCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<VetReminderDto>("Los recordatorios veterinarios requieren el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<VetReminderDto>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<VetReminderDto>("Acceso denegado.");

        var reminder = VetReminder.Create(
            request.PetId, request.RequestingUserId, request.Type,
            request.DueDate, request.Title, request.Notes);

        await medicalRepository.AddReminderAsync(reminder, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(VetReminderDto.FromDomain(reminder));
    }
}

// ── Delete vet reminder ───────────────────────────────────────────────────────

public sealed record DeleteVetReminderCommand(Guid ReminderId, Guid RequestingUserId)
    : IRequest<Result<Unit>>;

public sealed class DeleteVetReminderCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteVetReminderCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteVetReminderCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<Unit>("Los recordatorios veterinarios requieren el plan Familia.");

        var reminder = await medicalRepository.GetReminderByIdAsync(request.ReminderId, ct);
        if (reminder is null) return Result.Failure<Unit>("Recordatorio no encontrado.");

        var pet = await petRepository.GetByIdAsync(reminder.PetId, ct);
        if (pet is null) return Result.Failure<Unit>("Mascota no encontrada.");

        var canDelete = reminder.OwnerId == request.RequestingUserId
            || await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct);
        if (!canDelete)
            return Result.Failure<Unit>("Solo el dueño del recordatorio puede eliminarlo.");

        medicalRepository.DeleteReminder(reminder);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(Unit.Value);
    }
}

// ── Weight history ────────────────────────────────────────────────────────────

public sealed record WeightEntryDto(
    DateOnly Date,
    decimal WeightKg,
    string Source,
    string? ClinicName);

public sealed record WeightReferenceDto(
    decimal MinKg,
    decimal MaxKg,
    string Label);

public sealed record WeightHistoryDto(
    IReadOnlyList<WeightEntryDto> Entries,
    WeightReferenceDto? Reference,
    string? WeightChangeAlert);

public sealed record GetWeightHistoryQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<WeightHistoryDto>>;

public sealed class GetWeightHistoryQueryValidator : AbstractValidator<GetWeightHistoryQuery>
{
    public GetWeightHistoryQueryValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}

public sealed class GetWeightHistoryQueryHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    ISubscriptionService subscriptionService,
    IFamilyRepository familyRepository)
    : IRequestHandler<GetWeightHistoryQuery, Result<WeightHistoryDto>>
{
    public async Task<Result<WeightHistoryDto>> Handle(
        GetWeightHistoryQuery request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<WeightHistoryDto>("El historial de peso requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<WeightHistoryDto>("Mascota no encontrada.");

        var canAccess = await FamilyAccessChecker.CanAccessPetAsync(
            pet.OwnerId, request.RequestingUserId, familyRepository, ct);
        if (!canAccess) return Result.Failure<WeightHistoryDto>("Acceso no autorizado.");

        var records = await medicalRepository.GetByPetIdAsync(request.PetId, ct);

        var entries = records
            .Where(r => r.WeightKg.HasValue)
            .OrderBy(r => r.Date)
            .Select(r => new WeightEntryDto(
                r.Date,
                r.WeightKg!.Value,
                r.ClinicId.HasValue ? "Clinic" : "Owner",
                r.ClinicName))
            .ToList();

        var reference = BreedWeightReference.Resolve(pet.Breed, pet.Species.ToString());
        WeightReferenceDto? referenceDto = reference is null ? null
            : new WeightReferenceDto(reference.MinKg, reference.MaxKg, reference.Label);

        // Alert when weight dropped or gained >15% over the last 90 days
        string? alert = null;
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));
        var recent = entries.Where(e => e.Date >= cutoff).ToList();
        if (recent.Count >= 2)
        {
            var first = recent[0].WeightKg;
            var last  = recent[^1].WeightKg;
            if (first > 0)
            {
                var delta = Math.Abs((last - first) / first);
                if (delta >= 0.15m)
                    alert = last < first
                        ? $"El peso bajó un {delta:P0} en los últimos 90 días. Consulta con tu veterinario."
                        : $"El peso subió un {delta:P0} en los últimos 90 días. Consulta con tu veterinario.";
            }
        }

        return Result.Success(new WeightHistoryDto(entries, referenceDto, alert));
    }
}
