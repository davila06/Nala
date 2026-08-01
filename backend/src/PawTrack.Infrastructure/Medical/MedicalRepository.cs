using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class MedicalRepository(PawTrackDbContext db) : IMedicalRepository
{
    public async Task<IReadOnlyList<MedicalRecord>> GetByPetIdAsync(Guid petId, CancellationToken ct = default) =>
        await db.MedicalRecords.AsNoTracking()
            .Where(r => r.PetId == petId)
            .OrderByDescending(r => r.Date)
            .ToListAsync(ct);

    public Task<MedicalRecord?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.MedicalRecords.AsTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(MedicalRecord record, CancellationToken ct = default) =>
        await db.MedicalRecords.AddAsync(record, ct);

    public void Update(MedicalRecord record) => db.MedicalRecords.Update(record);
    public void Delete(MedicalRecord record) => db.MedicalRecords.Remove(record);

    public async Task<IReadOnlyList<VetReminder>> GetUpcomingRemindersAsync(Guid petId, CancellationToken ct = default) =>
        await db.VetReminders.AsNoTracking()
            .Where(r => r.PetId == petId && !r.IsCompleted)
            .OrderBy(r => r.DueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<VetReminder>> GetRemindersDueSoonAsync(
        DateOnly today, int daysAhead, CancellationToken ct = default)
    {
        var cutoff = today.AddDays(daysAhead);
        return await db.VetReminders.AsNoTracking()
            .Where(r => !r.IsCompleted && r.DueDate >= today && r.DueDate <= cutoff && r.ReminderSentAt == null)
            .ToListAsync(ct);
    }

    public Task<VetReminder?> GetReminderByIdAsync(Guid id, CancellationToken ct = default) =>
        db.VetReminders.AsTracking().FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddReminderAsync(VetReminder reminder, CancellationToken ct = default) =>
        await db.VetReminders.AddAsync(reminder, ct);

    public void UpdateReminder(VetReminder reminder) => db.VetReminders.Update(reminder);
}
