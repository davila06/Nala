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
}
