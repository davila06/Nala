using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public sealed record ClinicSiteAccessMemberReadModel(
    Guid UserId,
    string Name,
    string Email,
    Guid GrantedByUserId,
    DateTimeOffset GrantedAt);

public interface IClinicSiteAccessManagementRepository
{
    Task<Guid?> GetOrganizationIdForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicOrganizationRole?> GetActiveRoleAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsActiveMemberAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<ClinicOrganizationSiteAccess?> GetActiveAccessAsync(Guid organizationId, Guid clinicId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasOtherActiveSiteAccessAsync(Guid organizationId, Guid clinicId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicOrganizationSiteAccess access, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicSiteAccessMemberReadModel>> ListActiveMembersAsync(
        Guid organizationId,
        Guid clinicId,
        int take = 100,
        CancellationToken cancellationToken = default);
}
