using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Bot;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Bot;

public sealed class WhatsAppIdempotencyRepository(PawTrackDbContext db) : IWhatsAppIdempotencyRepository
{
    public async Task<bool> TryMarkAsync(string wamid, CancellationToken ct = default)
    {
        try
        {
            db.WhatsAppProcessedMessages.Add(WhatsAppProcessedMessage.Create(wamid));
            await db.SaveChangesAsync(ct);
            return true; // first delivery
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true ||
            ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Duplicate wamid — Meta re-delivery; skip processing.
            return false;
        }
    }
}
