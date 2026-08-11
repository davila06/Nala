using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IMedicalRepository
{
    Task<IReadOnlyList<MedicalRecord>> GetByPetIdAsync(Guid petId, CancellationToken ct = default);
    Task<MedicalRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(MedicalRecord record, CancellationToken ct = default);
    void Update(MedicalRecord record);
    void Delete(MedicalRecord record);

    Task<IReadOnlyList<VetReminder>> GetUpcomingRemindersAsync(Guid petId, CancellationToken ct = default);
    Task<IReadOnlyList<VetReminder>> GetRemindersDueSoonAsync(DateOnly today, int daysAhead, CancellationToken ct = default);
    Task<VetReminder?> GetReminderByIdAsync(Guid id, CancellationToken ct = default);
    Task AddReminderAsync(VetReminder reminder, CancellationToken ct = default);
    void UpdateReminder(VetReminder reminder);
    void DeleteReminder(VetReminder reminder);

    Task<IReadOnlyList<HealthProtocol>> GetHealthProtocolsBySpeciesAsync(string species, CancellationToken ct = default);
    /// <summary>Returns distinct PetIds that have at least one medical record — used by the health alert job.</summary>
    Task<IReadOnlyList<Guid>> GetPetIdsWithRecordsAsync(CancellationToken ct = default);
    /// <summary>Returns the most recent VetReminder of a given type for a pet, or null.</summary>
    Task<VetReminder?> GetLatestReminderByTypeAsync(Guid petId, MedicalRecordType type, CancellationToken ct = default);
}
