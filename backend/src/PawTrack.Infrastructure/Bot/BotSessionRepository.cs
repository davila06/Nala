using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Bot;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Bot;

public sealed class BotSessionRepository(PawTrackDbContext db) : IBotSessionRepository
{
    public async Task<BotSession?> GetActiveByPhoneHashAsync(
        string phoneNumberHash, CancellationToken ct = default) =>
        await db.BotSessions
            .AsTracking()
            .Where(s => s.PhoneNumberHash == phoneNumberHash
                     && s.Step != BotStep.Completed
                     && s.Step != BotStep.Expired)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(BotSession session, CancellationToken ct = default) =>
        await db.BotSessions.AddAsync(session, ct);

    public Task UpdateAsync(BotSession session, CancellationToken ct = default)
    {
        db.BotSessions.Update(session);
        return Task.CompletedTask;
    }
}
