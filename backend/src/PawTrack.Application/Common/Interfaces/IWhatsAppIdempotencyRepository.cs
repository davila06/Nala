using PawTrack.Domain.Bot;

namespace PawTrack.Application.Common.Interfaces;

public interface IWhatsAppIdempotencyRepository
{
    /// <summary>
    /// Attempts to insert <paramref name="wamid"/> as a processed message.
    /// Returns <c>true</c> if the insert succeeded (first delivery).
    /// Returns <c>false</c> if the wamid already exists (duplicate — skip processing).
    /// </summary>
    Task<bool> TryMarkAsync(string wamid, CancellationToken ct = default);
}
