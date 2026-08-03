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
    string Source)  // "Owner" | "Clinic"
{
    public static MedicalRecordDto FromDomain(MedicalRecord r) => new(
        r.Id, r.PetId, r.Type.ToString(), r.Date,
        r.Description, r.VetName, r.ClinicName,
        r.NextDueDate, r.DocumentUrl, r.CreatedAt,
        r.ClinicId,
        r.ClinicId.HasValue ? "Clinic" : "Owner");
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
    string? DocumentContentType) : IRequest<Result<MedicalRecordDto>>;

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
            request.VetName, request.ClinicName, request.NextDueDate);

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

public sealed record GetMedicalHistoryQuery(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<MedicalRecordDto>>>;

public sealed class GetMedicalHistoryQueryHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetMedicalHistoryQuery, Result<IReadOnlyList<MedicalRecordDto>>>
{
    public async Task<Result<IReadOnlyList<MedicalRecordDto>>> Handle(
        GetMedicalHistoryQuery request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<IReadOnlyList<MedicalRecordDto>>("El historial médico requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<IReadOnlyList<MedicalRecordDto>>("Mascota no encontrada.");
        if (!await FamilyAccessChecker.CanAccessPetAsync(pet.OwnerId, request.RequestingUserId, familyRepository, ct))
            return Result.Failure<IReadOnlyList<MedicalRecordDto>>("Acceso denegado.");

        var records = await medicalRepository.GetByPetIdAsync(request.PetId, ct);
        return Result.Success<IReadOnlyList<MedicalRecordDto>>(
            records.Select(MedicalRecordDto.FromDomain).ToList());
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
