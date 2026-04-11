using PawTrack.Domain.Fosters;

namespace PawTrack.Application.Common.Interfaces;

public interface ICustodyRecordRepository
{
    Task AddAsync(CustodyRecord custodyRecord, CancellationToken cancellationToken = default);

    Task<CustodyRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Update(CustodyRecord custodyRecord);
}
