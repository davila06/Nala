using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VaccinePassportRepository(PawTrackDbContext dbContext) : IVaccinePassportRepository
{
    public Task<VaccinePassport?> GetByCertificateIdAsync(Guid certificateId, CancellationToken cancellationToken = default) =>
        dbContext.VaccinePassports
            .AsNoTracking()
            .Include(passport => passport.Vaccines)
            .FirstOrDefaultAsync(passport => passport.CertificateId == certificateId, cancellationToken);

    public async Task AddAsync(VaccinePassport passport, CancellationToken cancellationToken = default) =>
        await dbContext.VaccinePassports.AddAsync(passport, cancellationToken);
}
