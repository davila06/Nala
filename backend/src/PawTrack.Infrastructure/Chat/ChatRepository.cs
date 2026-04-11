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
        CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.ChatMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.SentAt)
            .ToListAsync(cancellationToken);

        return messages.AsReadOnly();
    }

    public Task<int> CountUnreadMessagesAsync(
        Guid threadId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        dbContext.ChatMessages.CountAsync(
            m => m.ThreadId == threadId
                 && m.SenderUserId != recipientUserId
                 && !m.IsReadByRecipient,
            cancellationToken);
}
