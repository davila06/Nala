using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Fosters;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Fosters;

public sealed class CustodyRecordRepository(PawTrackDbContext dbContext) : ICustodyRecordRepository
{
    public async Task AddAsync(CustodyRecord custodyRecord, CancellationToken cancellationToken = default) =>
        await dbContext.Set<CustodyRecord>().AddAsync(custodyRecord, cancellationToken);

    public async Task<CustodyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Set<CustodyRecord>()
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Update(CustodyRecord custodyRecord) =>
        dbContext.Set<CustodyRecord>().Update(custodyRecord);
}
