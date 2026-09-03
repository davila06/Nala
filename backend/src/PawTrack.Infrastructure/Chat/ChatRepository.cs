using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Chat;

public sealed class ChatRepository(PawTrackDbContext dbContext) : IChatRepository
{
    // ── Threads ────────────────────────────────────────────────────────────────

    public Task<ChatThread?> GetThreadByIdAsync(Guid threadId, CancellationToken cancellationToken = default) =>
        dbContext.ChatThreads
                 .Include(t => t.Messages)
                 .FirstOrDefaultAsync(t => t.Id == threadId, cancellationToken);

    public Task<bool> ThreadExistsAsync(
        Guid lostPetEventId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChatThreads.AnyAsync(
            t => t.LostPetEventId == lostPetEventId && t.InitiatorUserId == initiatorUserId,
            cancellationToken);

    public async Task<IReadOnlyList<ChatThread>> GetThreadsByLostPetEventAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken = default)
    {
        var threads = await dbContext.ChatThreads
            .AsNoTracking()
            .Where(t => t.LostPetEventId == lostPetEventId)
            .OrderByDescending(t => t.LastMessageAt)
            .ToListAsync(cancellationToken);

        return threads.AsReadOnly();
    }

    public async Task<IReadOnlyList<ChatThread>> GetThreadsByLostPetEventAndParticipantAsync(
        Guid lostPetEventId,
        Guid participantUserId,
        CancellationToken cancellationToken = default)
    {
        var threads = await dbContext.ChatThreads
            .AsNoTracking()
            .Where(t => t.LostPetEventId == lostPetEventId
                        && (t.InitiatorUserId == participantUserId || t.OwnerUserId == participantUserId))
            .OrderByDescending(t => t.LastMessageAt)
            .ToListAsync(cancellationToken);

        return threads.AsReadOnly();
    }

    public async Task<IReadOnlyList<ChatThread>> GetThreadsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var threads = await dbContext.ChatThreads
            .AsNoTracking()
            .Where(t => t.InitiatorUserId == userId || t.OwnerUserId == userId)
            .OrderByDescending(t => t.LastMessageAt)
            .ToListAsync(cancellationToken);

        return threads.AsReadOnly();
    }

    public Task AddThreadAsync(ChatThread thread, CancellationToken cancellationToken = default) =>
        dbContext.ChatThreads.AddAsync(thread, cancellationToken).AsTask();

    public void UpdateThread(ChatThread thread) =>
        dbContext.ChatThreads.Update(thread);

    // ── Messages ───────────────────────────────────────────────────────────────

    public Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default) =>
        dbContext.ChatMessages.AddAsync(message, cancellationToken).AsTask();

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesByThreadAsync(
        Guid threadId,
        Guid? beforeMessageId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ChatMessage> query = dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.ThreadId == threadId);

        // When a cursor is provided, restrict to messages sent BEFORE that message's
        // timestamp so the client can page backward (infinite scroll upward).
        if (beforeMessageId.HasValue)
        {
            var cursorTime = await dbContext.ChatMessages
                .AsNoTracking()
                .Where(m => m.Id == beforeMessageId.Value)
                .Select(m => m.SentAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorTime != default)
                query = query.Where(m => m.SentAt < cursorTime);
        }

        // Fetch the most recent `pageSize` messages, then reverse for ascending
        // chronological render order (oldest first within the returned page).
        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        messages.Reverse();
        return messages.AsReadOnly();
    }

    public Task MarkMessagesAsReadAsync(
        IReadOnlyList<Guid> messageIds,
        CancellationToken cancellationToken = default) =>
        dbContext.ChatMessages
            .Where(m => messageIds.Contains(m.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsReadByRecipient, true),
                cancellationToken);

    public Task<int> CountUnreadMessagesAsync(
        Guid threadId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChatMessages.CountAsync(
            m => m.ThreadId == threadId
                 && m.SenderUserId != recipientUserId
                 && !m.IsReadByRecipient,
            cancellationToken);

    public async Task<int> DeleteClosedThreadsOlderThanAsync(
        DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        var threadIds = await dbContext.ChatThreads
            .Where(t => t.Status == ChatThreadStatus.Closed && t.LastMessageAt < cutoff)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        if (threadIds.Count == 0) return 0;

        // Delete messages first — no FK cascade is assumed here to keep the operation explicit.
        await dbContext.ChatMessages
            .Where(m => threadIds.Contains(m.ThreadId))
            .ExecuteDeleteAsync(cancellationToken);

        return await dbContext.ChatThreads
            .Where(t => threadIds.Contains(t.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}
