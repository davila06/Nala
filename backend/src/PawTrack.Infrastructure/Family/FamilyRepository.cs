using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Family;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Family;

public sealed class FamilyRepository(PawTrackDbContext db) : IFamilyRepository
{
    public Task<FamilyAccount?> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default) =>
        db.FamilyAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.OwnerId == ownerId, ct);

    public Task<FamilyAccount?> GetByMemberAsync(Guid userId, CancellationToken ct = default) =>
        db.FamilyMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .Join(db.FamilyAccounts, m => m.FamilyAccountId, a => a.Id, (_, a) => a)
            .FirstOrDefaultAsync(ct);

    public Task<FamilyAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.FamilyAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<FamilyMembership>> GetActiveMembershipsAsync(
        Guid familyAccountId, CancellationToken ct = default) =>
        await db.FamilyMemberships
            .AsNoTracking()
            .Where(m => m.FamilyAccountId == familyAccountId && m.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetActiveMemberIdsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var account = await GetByOwnerAsync(ownerId, ct);
        if (account is null) return [];
        var memberships = await GetActiveMembershipsAsync(account.Id, ct);
        return memberships.Select(m => m.UserId).ToList();
    }

    public Task<int> CountActiveMembersAsync(Guid familyAccountId, CancellationToken ct = default) =>
        db.FamilyMemberships.CountAsync(m => m.FamilyAccountId == familyAccountId && m.IsActive, ct);

    public Task<FamilyInvitation?> GetInvitationByTokenAsync(Guid token, CancellationToken ct = default) =>
        db.FamilyInvitations.AsTracking().FirstOrDefaultAsync(i => i.Token == token, ct);

    public async Task AddAccountAsync(FamilyAccount account, CancellationToken ct = default) =>
        await db.FamilyAccounts.AddAsync(account, ct);

    public async Task AddMembershipAsync(FamilyMembership membership, CancellationToken ct = default) =>
        await db.FamilyMemberships.AddAsync(membership, ct);

    public async Task AddInvitationAsync(FamilyInvitation invitation, CancellationToken ct = default) =>
        await db.FamilyInvitations.AddAsync(invitation, ct);

    public void UpdateMembership(FamilyMembership membership) =>
        db.FamilyMemberships.Update(membership);

    public void UpdateInvitation(FamilyInvitation invitation) =>
        db.FamilyInvitations.Update(invitation);
}
