using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicCrmRepository(PawTrackDbContext db) : IClinicCrmRepository
{
    public Task<ClinicClientCommunicationActivity?> GetActivityByRequestIdAsync(Guid clinicId, Guid requestId, CancellationToken cancellationToken = default) =>
        db.ClinicClientCommunicationActivities.AsNoTracking()
            .FirstOrDefaultAsync(activity => activity.ClinicId == clinicId && activity.RequestId == requestId, cancellationToken);

    public Task<int> DeleteActivitiesBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.ClinicClientCommunicationActivities.Where(activity => activity.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public Task<int> DeleteCompletedTasksBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.ClinicCrmTasks.Where(task => task.Status == ClinicCrmTaskStatus.Completed && task.CompletedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>> GetOwnerPreferencesForPetAsync(Guid petId, CancellationToken cancellationToken = default) =>
        await (from clinic in db.Clinics.AsNoTracking()
               join appointment in db.VeterinarianAppointments.AsNoTracking() on clinic.Id equals appointment.ClinicId
               join preference in db.ClinicClientCommunicationPreferences.AsNoTracking().Where(x => x.PetId == petId)
                   on clinic.Id equals preference.ClinicId into consent
               from preference in consent.DefaultIfEmpty()
               where appointment.PetId == petId && clinic.Status == ClinicStatus.Active
               select new OwnerClinicCommunicationPreferenceReadModel(
                   clinic.Id, clinic.Name, preference == null ? null : preference.Channel.ToString(),
                   preference == null ? null : preference.Purpose.ToString(), preference != null && preference.IsOptedIn))
            .Distinct().Take(100).ToListAsync(cancellationToken);

    public Task<bool> HasClinicPatientRelationshipAsync(Guid clinicId, Guid petId, CancellationToken cancellationToken = default) =>
        db.VeterinarianAppointments.AsNoTracking().AnyAsync(x => x.ClinicId == clinicId && x.PetId == petId, cancellationToken);

    public Task<ClinicClientCommunicationPreference?> GetPreferenceAsync(
        Guid clinicId,
        Guid petId,
        ClinicCommunicationChannel channel,
        ClinicCommunicationPurpose purpose,
        CancellationToken cancellationToken = default) =>
        db.ClinicClientCommunicationPreferences.AsTracking()
            .FirstOrDefaultAsync(x => x.ClinicId == clinicId && x.PetId == petId && x.Channel == channel && x.Purpose == purpose, cancellationToken);

    public Task<ClinicCrmTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        db.ClinicCrmTasks.AsTracking().FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);

    public async Task AddPreferenceAsync(ClinicClientCommunicationPreference preference, CancellationToken cancellationToken = default) =>
        await db.ClinicClientCommunicationPreferences.AddAsync(preference, cancellationToken);

    public async Task AddActivityAsync(ClinicClientCommunicationActivity activity, CancellationToken cancellationToken = default) =>
        await db.ClinicClientCommunicationActivities.AddAsync(activity, cancellationToken);

    public async Task AddTaskAsync(ClinicCrmTask task, CancellationToken cancellationToken = default) =>
        await db.ClinicCrmTasks.AddAsync(task, cancellationToken);

    public void UpdatePreference(ClinicClientCommunicationPreference preference) => db.ClinicClientCommunicationPreferences.Update(preference);
    public void UpdateTask(ClinicCrmTask task) => db.ClinicCrmTasks.Update(task);

    public async Task<ClinicCrmDashboardReadModel> GetDashboardAsync(Guid clinicId, DateOnly today, CancellationToken cancellationToken = default)
    {
        var petIds = await GetClinicPetIds(clinicId, cancellationToken);
        var preferences = await (
            from preference in db.ClinicClientCommunicationPreferences.AsNoTracking()
            join pet in db.Pets.AsNoTracking() on preference.PetId equals pet.Id
            join owner in db.Users.AsNoTracking() on preference.OwnerUserId equals owner.Id
            where preference.ClinicId == clinicId
            orderby preference.UpdatedAt descending
            select new ClinicCrmPreferenceReadModel(preference.Id, preference.PetId, pet.Name, preference.OwnerUserId, owner.Name, preference.Channel, preference.Purpose, preference.IsOptedIn, preference.ConsentSource, preference.UpdatedAt))
            .Take(100)
            .ToListAsync(cancellationToken);

        var activities = await (
            from activity in db.ClinicClientCommunicationActivities.AsNoTracking()
            join pet in db.Pets.AsNoTracking() on activity.PetId equals pet.Id
            join owner in db.Users.AsNoTracking() on activity.OwnerUserId equals owner.Id
            where activity.ClinicId == clinicId
            orderby activity.CreatedAt descending
            select new ClinicCrmActivityReadModel(activity.Id, activity.PetId, pet.Name, activity.OwnerUserId, owner.Name, activity.Channel, activity.Purpose, activity.Direction, activity.Status, activity.Subject, activity.Body, activity.CreatedAt))
            .Take(50)
            .ToListAsync(cancellationToken);

        var tasks = await (
            from task in db.ClinicCrmTasks.AsNoTracking()
            join pet in db.Pets.AsNoTracking() on task.PetId equals pet.Id
            join owner in db.Users.AsNoTracking() on task.OwnerUserId equals owner.Id
            where task.ClinicId == clinicId && task.Status == ClinicCrmTaskStatus.Open
            orderby task.DueDate, task.CreatedAt descending
            select new ClinicCrmTaskReadModel(task.Id, task.PetId, pet.Name, task.OwnerUserId, owner.Name, task.Type, task.Status, task.DueDate, task.Title, task.Notes))
            .Take(100)
            .ToListAsync(cancellationToken);

        var segments = await BuildSegmentsAsync(clinicId, petIds, today, cancellationToken);
        return new ClinicCrmDashboardReadModel(preferences, activities, tasks, segments);
    }

    private async Task<IReadOnlyList<Guid>> GetClinicPetIds(Guid clinicId, CancellationToken cancellationToken)
    {
        var appointmentPetIds = db.VeterinarianAppointments.AsNoTracking()
            .Where(appointment => appointment.ClinicId == clinicId)
            .Select(appointment => appointment.PetId);
        var consultationPetIds = db.ClinicalConsultations.AsNoTracking()
            .Where(consultation => consultation.ClinicId == clinicId)
            .Select(consultation => consultation.PetId);
        var salePetIds = db.ClinicSales.AsNoTracking()
            .Where(sale => sale.ClinicId == clinicId && sale.PetId.HasValue)
            .Select(sale => sale.PetId!.Value);
        var crmPetIds = db.ClinicCrmTasks.AsNoTracking()
            .Where(task => task.ClinicId == clinicId)
            .Select(task => task.PetId);
        return await appointmentPetIds.Concat(consultationPetIds).Concat(salePetIds).Concat(crmPetIds)
            .Distinct()
            .Take(1000)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ClinicCrmSegmentReadModel>> BuildSegmentsAsync(Guid clinicId, IReadOnlyList<Guid> petIds, DateOnly today, CancellationToken cancellationToken)
    {
        if (petIds.Count == 0)
            return [];
        var dueSoon = today.AddDays(30);
        var inactiveSince = DateTimeOffset.UtcNow.AddDays(-180);
        var followUpPetIds = await db.ClinicCrmTasks.AsNoTracking()
            .Where(task => task.ClinicId == clinicId && task.Type == ClinicCrmTaskType.FollowUpTreatment
                && task.Status == ClinicCrmTaskStatus.Open && task.DueDate <= dueSoon)
            .Select(task => task.PetId)
            .Distinct()
            .Take(500)
            .ToListAsync(cancellationToken);
        var activeAppointmentPetIds = await db.VeterinarianAppointments.AsNoTracking()
            .Where(appointment => appointment.ClinicId == clinicId && appointment.StartsAt >= inactiveSince && appointment.StartsAt <= DateTimeOffset.UtcNow)
            .Select(appointment => appointment.PetId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var geriatricPetIds = await db.Pets.AsNoTracking()
            .Where(pet => petIds.Contains(pet.Id) && pet.BirthDate.HasValue && pet.BirthDate.Value <= today.AddYears(-8))
            .Select(pet => pet.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        var inactivePetIds = petIds.Except(activeAppointmentPetIds).Take(500).ToArray();
        return
        [
            new ClinicCrmSegmentReadModel("followups-due", "Seguimientos próximos", followUpPetIds.Count, followUpPetIds),
            new ClinicCrmSegmentReadModel("inactive-clients", "Clientes sin visita en 180 días", inactivePetIds.Length, inactivePetIds),
            new ClinicCrmSegmentReadModel("geriatric-pets", "Pacientes senior", geriatricPetIds.Count, geriatricPetIds),
            new ClinicCrmSegmentReadModel("open-followups", "Seguimientos abiertos", await db.ClinicCrmTasks.AsNoTracking().CountAsync(task => task.ClinicId == clinicId && task.Status == ClinicCrmTaskStatus.Open, cancellationToken), []),
        ];
    }
}
