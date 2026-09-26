using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicSiteAccessRepository(PawTrackDbContext db) : IClinicSiteAccessRepository
{
    public async Task<bool> HasAccessAsync(Guid userId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (await db.ClinicOrganizationSiteAccess.AsNoTracking().AnyAsync(access =>
                access.UserId == userId && access.ClinicId == clinicId && !access.IsRevoked,
                cancellationToken))
            return true;

        if (await db.ClinicStaffMemberships.AsNoTracking().AnyAsync(member =>
                member.UserId == userId && member.ClinicId == clinicId && !member.IsRevoked,
                cancellationToken))
            return true;

        return await db.ClinicFinanceMemberships.AsNoTracking().AnyAsync(member =>
            member.UserId == userId && member.ClinicId == clinicId && !member.IsRevoked,
            cancellationToken);
    }

    public async Task<IReadOnlyList<AccessibleClinicSiteReadModel>> ListAccessibleSitesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await (from access in db.ClinicOrganizationSiteAccess.AsNoTracking()
               join site in db.ClinicOrganizationSites.AsNoTracking()
                   on new { access.OrganizationId, access.ClinicId }
                   equals new { site.OrganizationId, site.ClinicId }
               join organization in db.ClinicOrganizations.AsNoTracking()
                   on access.OrganizationId equals organization.Id
               join clinic in db.Clinics.AsNoTracking()
                   on access.ClinicId equals clinic.Id
               where access.UserId == userId && !access.IsRevoked
               orderby organization.Name, site.IsPrimary descending, clinic.Name
               select new AccessibleClinicSiteReadModel(
                   organization.Id,
                   organization.Name,
                   clinic.Id,
                   clinic.Name,
                   site.IsPrimary))
            .Take(100)
            .ToListAsync(cancellationToken);
}
