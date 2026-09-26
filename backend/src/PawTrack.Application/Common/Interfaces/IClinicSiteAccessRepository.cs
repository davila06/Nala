namespace PawTrack.Application.Common.Interfaces;

public sealed record AccessibleClinicSiteReadModel(
    Guid OrganizationId,
    string OrganizationName,
    Guid ClinicId,
    string ClinicName,
    bool IsPrimary);

public interface IClinicSiteAccessRepository
{
    Task<bool> HasAccessAsync(Guid userId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<Guid?> GetActiveClinicIdAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<bool> SetActiveClinicIdAsync(Guid userId, Guid sessionId, Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccessibleClinicSiteReadModel>> ListAccessibleSitesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
