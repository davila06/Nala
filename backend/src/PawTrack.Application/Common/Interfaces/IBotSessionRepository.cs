using PawTrack.Domain.Bot;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Persistence contract for <see cref="BotSession"/> entities.
/// </summary>
public interface IBotSessionRepository
{
    /// <summary>
    /// Returns the most recent non-expired, non-completed session for the given
    /// phone-number hash, or <c>null</c> if none exists.
    /// </summary>
    Task<BotSession?> GetActiveByPhoneHashAsync(string phoneNumberHash, CancellationToken ct = default);

    Task AddAsync(BotSession session, CancellationToken ct = default);

    Task UpdateAsync(BotSession session, CancellationToken ct = default);
}
