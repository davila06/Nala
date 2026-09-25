using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public sealed record ClinicCrmPreferenceReadModel(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, ClinicCommunicationChannel Channel, ClinicCommunicationPurpose Purpose, bool IsOptedIn, string ConsentSource, DateTimeOffset UpdatedAt);
public sealed record ClinicCrmActivityReadModel(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, ClinicCommunicationChannel Channel, ClinicCommunicationPurpose Purpose, ClinicCommunicationDirection Direction, ClinicCommunicationStatus Status, string Subject, string Body, DateTimeOffset CreatedAt);
public sealed record ClinicCrmTaskReadModel(Guid Id, Guid PetId, string PetName, Guid OwnerUserId, string OwnerName, ClinicCrmTaskType Type, ClinicCrmTaskStatus Status, DateOnly DueDate, string Title, string? Notes);
public sealed record ClinicCrmSegmentReadModel(string Key, string Label, int Count, IReadOnlyList<Guid> PetIds);
public sealed record ClinicCrmDashboardReadModel(IReadOnlyList<ClinicCrmPreferenceReadModel> Preferences, IReadOnlyList<ClinicCrmActivityReadModel> RecentActivities, IReadOnlyList<ClinicCrmTaskReadModel> OpenTasks, IReadOnlyList<ClinicCrmSegmentReadModel> Segments);
public sealed record OwnerClinicCommunicationPreferenceReadModel(Guid ClinicId, string ClinicName, string? Channel, string? Purpose, bool IsOptedIn);

public interface IClinicCrmRepository
{
    Task<int> DeleteActivitiesBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task<int> DeleteCompletedTasksBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task<ClinicClientCommunicationActivity?> GetActivityByRequestIdAsync(Guid clinicId, Guid requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerClinicCommunicationPreferenceReadModel>> GetOwnerPreferencesForPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<bool> HasClinicPatientRelationshipAsync(Guid clinicId, Guid petId, CancellationToken cancellationToken = default);
    Task<ClinicClientCommunicationPreference?> GetPreferenceAsync(Guid clinicId, Guid petId, ClinicCommunicationChannel channel, ClinicCommunicationPurpose purpose, CancellationToken cancellationToken = default);
    Task<ClinicCrmTask?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task AddPreferenceAsync(ClinicClientCommunicationPreference preference, CancellationToken cancellationToken = default);
    Task AddActivityAsync(ClinicClientCommunicationActivity activity, CancellationToken cancellationToken = default);
    Task AddTaskAsync(ClinicCrmTask task, CancellationToken cancellationToken = default);
    void UpdatePreference(ClinicClientCommunicationPreference preference);
    void UpdateTask(ClinicCrmTask task);
    Task<ClinicCrmDashboardReadModel> GetDashboardAsync(Guid clinicId, DateOnly today, CancellationToken cancellationToken = default);
}
