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

    /// <summary>
    /// Cursor-based scan history for a pet. Pass <paramref name="afterId"/> to advance the cursor.
    /// Avoids OFFSET degradation when scan history grows large.
    /// </summary>
    Task<IReadOnlyList<QrScanEvent>> GetByPetIdAfterCursorAsync(
        Guid petId,
        Guid? afterId,
        int take,
        CancellationToken cancellationToken = default);
}
