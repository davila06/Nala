using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicScanRepository(PawTrackDbContext dbContext) : IClinicScanRepository
{
    public async Task AddAsync(ClinicScan scan, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicScans.AddAsync(scan, cancellationToken);
}
