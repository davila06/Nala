using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Medical;

/// <summary>Implemented in Infrastructure using QuestPDF to keep the dependency out of the Application layer.</summary>
public interface IMedicalPdfExporter
{
    Task<byte[]> ExportAsync(
        string petName,
        IReadOnlyList<MedicalRecordDto> records,
        IReadOnlyList<VetReminderDto> reminders,
        CancellationToken ct = default);
}

public sealed record ExportMedicalHistoryCommand(Guid PetId, Guid RequestingUserId) : IRequest<Result<byte[]>>;

public sealed class ExportMedicalHistoryCommandHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IMedicalPdfExporter pdfExporter)
    : IRequestHandler<ExportMedicalHistoryCommand, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(ExportMedicalHistoryCommand request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<byte[]>("La exportación de historial médico requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<byte[]>("Mascota no encontrada.");

        // Allow family members to export
        var canAccess = pet.OwnerId == request.RequestingUserId
            || (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId);
        if (!canAccess) return Result.Failure<byte[]>("Acceso denegado.");

        var records = (await medicalRepository.GetByPetIdAsync(request.PetId, ct))
            .Select(MedicalRecordDto.FromDomain).ToList();
        var reminders = (await medicalRepository.GetUpcomingRemindersAsync(request.PetId, ct))
            .Select(VetReminderDto.FromDomain).ToList();

        var bytes = await pdfExporter.ExportAsync(pet.Name, records, reminders, ct);
        return Result.Success(bytes);
    }
}
