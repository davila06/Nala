using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public sealed record ClinicStaffMemberReadModel(Guid UserId, string Email, ClinicStaffRole Role, Guid? VeterinarianId, bool IsRevoked);
public sealed record ClinicStaffWorkspaceReadModel(Guid ClinicId, string ClinicName, ClinicStaffRole Role);

public interface IClinicStaffAccessRepository
{
    Task<bool> HasPermissionAsync(Guid clinicId, Guid userId, ClinicStaffPermission permission, CancellationToken cancellationToken = default);
    Task<ClinicStaffMembership?> GetAsync(Guid clinicId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicStaffMemberReadModel>> ListMembersAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicStaffWorkspaceReadModel>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicStaffMembership member, CancellationToken cancellationToken = default);
}