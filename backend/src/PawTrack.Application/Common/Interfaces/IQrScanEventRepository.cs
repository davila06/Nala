using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IQrScanEventRepository
{
    Task AddAsync(QrScanEvent scanEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QrScanEvent>> GetByPetIdAsync(
        Guid petId,
        int take,
        CancellationToken cancellationToken = default);

    Task<bool> HasScanForPetOnDateAsync(
        Guid petId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default);

    Task<bool> HasScanForPetSinceAsync(
        Guid petId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all QrScanEvent records with <see cref="QrScanEvent.ScannedAt"/> older than
    /// <paramref name="cutoff"/> and returns the number of rows deleted.
    /// </summary>
    Task<int> DeleteBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
}
