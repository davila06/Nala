using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicSiteAccessRepository(PawTrackDbContext db) : IClinicSiteAccessRepository
{
    public async Task<bool> HasAccessAsync(Guid userId, Guid clinicId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || clinicId == Guid.Empty) return false;

        return await db.Clinics.AsNoTracking().AnyAsync(clinic =>
            clinic.Id == clinicId &&
            (db.ClinicOrganizationSiteAccess.Any(access =>
                 access.UserId == userId && access.ClinicId == clinicId && !access.IsRevoked)
             || db.ClinicStaffMemberships.Any(member =>
                 member.UserId == userId && member.ClinicId == clinicId && !member.IsRevoked)
             || db.ClinicFinanceMemberships.Any(member =>
                 member.UserId == userId && member.ClinicId == clinicId && !member.IsRevoked)),
            cancellationToken);
    }

    public async Task<Guid?> GetActiveClinicIdAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var clinicId = await db.RefreshTokens.AsNoTracking()
            .Where(token => token.UserId == userId && token.SessionId == sessionId
                && !token.IsRevoked && token.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => token.ActiveClinicId)
            .FirstOrDefaultAsync(cancellationToken);

        return clinicId.HasValue && await HasAccessAsync(userId, clinicId.Value, cancellationToken)
            ? clinicId
            : null;
    }

    public async Task<bool> SetActiveClinicIdAsync(
        Guid userId,
        Guid sessionId,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (!await HasAccessAsync(userId, clinicId, cancellationToken)) return false;

        var tokens = await db.RefreshTokens
            .Where(token => token.UserId == userId && token.SessionId == sessionId
                && !token.IsRevoked && token.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);
        if (tokens.Count == 0) return false;

        foreach (var token in tokens)
            token.SelectActiveClinic(clinicId);
        return true;
    }

    public async Task<IReadOnlyList<AccessibleClinicSiteReadModel>> ListAccessibleSitesAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await (from site in db.ClinicOrganizationSites.AsNoTracking()
               join organization in db.ClinicOrganizations.AsNoTracking()
                   on site.OrganizationId equals organization.Id
               join clinic in db.Clinics.AsNoTracking()
                   on site.ClinicId equals clinic.Id
               where db.ClinicOrganizationSiteAccess.Any(access =>
                         access.UserId == userId && access.ClinicId == site.ClinicId && !access.IsRevoked)
                     || db.ClinicStaffMemberships.Any(member =>
                         member.UserId == userId && member.ClinicId == site.ClinicId && !member.IsRevoked)
                     || db.ClinicFinanceMemberships.Any(member =>
                         member.UserId == userId && member.ClinicId == site.ClinicId && !member.IsRevoked)
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
