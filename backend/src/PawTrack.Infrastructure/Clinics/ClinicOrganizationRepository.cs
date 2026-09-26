using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicOrganizationRepository(PawTrackDbContext dbContext) : IClinicOrganizationRepository
{
    public async Task AddAsync(ClinicOrganization organization, CancellationToken cancellationToken = default)
    {
        await dbContext.ClinicOrganizations.AddAsync(organization, cancellationToken);
        await dbContext.ClinicOrganizationMemberships.AddRangeAsync(organization.Memberships, cancellationToken);
        await dbContext.ClinicOrganizationSites.AddRangeAsync(organization.Sites, cancellationToken);
    }
}
