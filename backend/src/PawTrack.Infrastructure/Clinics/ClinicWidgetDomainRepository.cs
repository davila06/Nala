using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicWidgetDomainRepository(PawTrackDbContext dbContext) : IClinicWidgetDomainRepository
{
    public async Task<IReadOnlyList<ClinicWidgetDomain>> GetForClinicAsync(
        Guid clinicId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ClinicWidgetDomain>().AsNoTracking()
            .Where(x => x.ClinicId == clinicId)
            .OrderBy(x => x.Domain)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveAsync(
        Guid clinicId, string domain, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClinicWidgetDomain>().AnyAsync(
            x => x.ClinicId == clinicId && x.IsActive && x.Domain == domain,
            cancellationToken);

    public async Task AddAsync(ClinicWidgetDomain domain, CancellationToken cancellationToken = default) =>
        await dbContext.Set<ClinicWidgetDomain>().AddAsync(domain, cancellationToken);

    public Task<ClinicWidgetDomain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<ClinicWidgetDomain>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Update(ClinicWidgetDomain domain) => dbContext.Set<ClinicWidgetDomain>().Update(domain);
}
