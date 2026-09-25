using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.Interfaces;

public interface IClinicFinanceAccessRepository
{
    Task<IReadOnlyList<ClinicFinanceWorkspaceDto>> ListWorkspacesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid clinicId, Guid userId, ClinicFinancePermission permission, CancellationToken cancellationToken = default);
    Task<ClinicFinanceMembership?> GetAsync(Guid clinicId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicFinanceMembership membership, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicFinanceMembership>> ListAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicFinanceMemberReadModel>> ListMemberDetailsAsync(Guid clinicId, CancellationToken cancellationToken = default);
}

public sealed record ClinicFinanceWorkspaceDto(Guid ClinicId, string ClinicName, string Role);
public sealed record ClinicFinanceMemberReadModel(Guid Id, Guid UserId, string Email, string Role, bool IsRevoked, DateTimeOffset GrantedAt);
