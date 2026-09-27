using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicSiteAccessManagementRepository(PawTrackDbContext db) : IClinicSiteAccessManagementRepository
{
    public Task<Guid?> GetOrganizationIdForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        db.ClinicOrganizationSites.AsNoTracking()
            .Where(site => site.ClinicId == clinicId)
            .Select(site => (Guid?)site.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<ClinicOrganizationRole?> GetActiveRoleAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.ClinicOrganizationMemberships.AsNoTracking()
            .Where(member => member.OrganizationId == organizationId && member.UserId == userId && !member.IsRevoked)
            .Select(member => (ClinicOrganizationRole?)member.Role)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> IsActiveMemberAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.ClinicOrganizationMemberships.AsNoTracking().AnyAsync(
            member => member.OrganizationId == organizationId && member.UserId == userId && !member.IsRevoked,
            cancellationToken);

    public Task<ClinicOrganizationSiteAccess?> GetActiveAccessAsync(
        Guid organizationId,
        Guid clinicId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.ClinicOrganizationSiteAccess.FirstOrDefaultAsync(
            access => access.OrganizationId == organizationId && access.ClinicId == clinicId
                && access.UserId == userId && !access.IsRevoked,
            cancellationToken);

    public Task<bool> HasOtherActiveSiteAccessAsync(
        Guid organizationId,
        Guid clinicId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        db.ClinicOrganizationSiteAccess.AsNoTracking().AnyAsync(
            access => access.OrganizationId == organizationId && access.UserId == userId
                && access.ClinicId != clinicId && !access.IsRevoked,
            cancellationToken);

    public async Task AddAsync(ClinicOrganizationSiteAccess access, CancellationToken cancellationToken = default) =>
        await db.ClinicOrganizationSiteAccess.AddAsync(access, cancellationToken);

    public async Task<IReadOnlyList<ClinicSiteAccessMemberReadModel>> ListActiveMembersAsync(
        Guid organizationId,
        Guid clinicId,
        int take = 100,
        CancellationToken cancellationToken = default) =>
        await (from access in db.ClinicOrganizationSiteAccess.AsNoTracking()
               join user in db.Users.AsNoTracking() on access.UserId equals user.Id
               where access.OrganizationId == organizationId && access.ClinicId == clinicId && !access.IsRevoked
               orderby access.GrantedAt descending
               select new ClinicSiteAccessMemberReadModel(
                   user.Id,
                   user.Name,
                   user.Email,
                   access.GrantedByUserId,
                   access.GrantedAt))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
}
