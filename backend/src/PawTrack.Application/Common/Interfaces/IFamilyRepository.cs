using PawTrack.Domain.Family;

namespace PawTrack.Application.Common.Interfaces;

public interface IFamilyRepository
{
    Task<FamilyAccount?> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<FamilyAccount?> GetByMemberAsync(Guid userId, CancellationToken ct = default);
    Task<FamilyAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyMembership>> GetActiveMembershipsAsync(Guid familyAccountId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetActiveMemberIdsAsync(Guid ownerId, CancellationToken ct = default);
    Task<int> CountActiveMembersAsync(Guid familyAccountId, CancellationToken ct = default);
    Task<FamilyInvitation?> GetInvitationByTokenAsync(Guid token, CancellationToken ct = default);
    Task AddAccountAsync(FamilyAccount account, CancellationToken ct = default);
    Task AddMembershipAsync(FamilyMembership membership, CancellationToken ct = default);
    Task AddInvitationAsync(FamilyInvitation invitation, CancellationToken ct = default);
    void UpdateMembership(FamilyMembership membership);
    void UpdateInvitation(FamilyInvitation invitation);
}
