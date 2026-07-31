using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class CertificateRepository(PawTrackDbContext dbContext) : ICertificateRepository
{
    public Task<VetCertificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.VetCertificates.FindAsync([id], cancellationToken).AsTask();

    public Task<VetCertificate?> GetByVerificationCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.VetCertificates.FirstOrDefaultAsync(c => c.VerificationCode == code, cancellationToken);

    public async Task<IReadOnlyList<VetCertificate>> GetForPetAsync(Guid petId, CancellationToken cancellationToken = default) =>
        await dbContext.VetCertificates
            .Where(c => c.PetId == petId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VetCertificate>> GetForClinicAsync(Guid clinicId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await dbContext.VetCertificates
            .Where(c => c.ClinicId == clinicId)
            .OrderByDescending(c => c.IssuedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(VetCertificate certificate, CancellationToken cancellationToken = default) =>
        await dbContext.VetCertificates.AddAsync(certificate, cancellationToken);

    public void Update(VetCertificate certificate) =>
        dbContext.VetCertificates.Update(certificate);
}
