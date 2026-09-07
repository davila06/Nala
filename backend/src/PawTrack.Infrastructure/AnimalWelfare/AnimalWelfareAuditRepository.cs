using Microsoft.EntityFrameworkCore;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.AnimalWelfare;

public sealed class AnimalWelfareAuditRepository(PawTrackDbContext dbContext) : IAnimalWelfareAuditRepository
{
    public async Task AddAsync(AnimalWelfareCaseAuditLog log, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCaseAuditLogs.AddAsync(log, cancellationToken);

    public async Task AddNoteAsync(AnimalWelfareCaseNote note, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCaseNotes.AddAsync(note, cancellationToken);

    public async Task AddReferralAsync(AnimalWelfareReferral referral, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareReferrals.AddAsync(referral, cancellationToken);

    public async Task<IReadOnlyList<AnimalWelfareCaseAuditLog>> GetAuditByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCaseAuditLogs.AsNoTracking()
            .Where(l => l.CaseId == caseId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AnimalWelfareCaseNote>> GetNotesByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        await dbContext.AnimalWelfareCaseNotes.AsNoTracking()
            .Where(n => n.CaseId == caseId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
}
