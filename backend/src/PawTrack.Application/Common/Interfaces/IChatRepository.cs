using PawTrack.Domain.Chat;

namespace PawTrack.Application.Common.Interfaces;

public interface IChatRepository
{
    Task<ChatThread?> GetThreadByIdAsync(Guid threadId, CancellationToken cancellationToken = default);

    Task<bool> ThreadExistsAsync(
        Guid lostPetEventId,
        Guid initiatorUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatThread>> GetThreadsByLostPetEventAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only threads for <paramref name="lostPetEventId"/> where
    /// <paramref name="participantUserId"/> is the initiator or owner.
    /// Filters at the database level to prevent timing-based BOLA — callers never
    /// receive thread metadata for events they are not a participant of.
    /// </summary>
    Task<IReadOnlyList<ChatThread>> GetThreadsByLostPetEventAndParticipantAsync(
        Guid lostPetEventId,
        Guid participantUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatThread>> GetThreadsByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddThreadAsync(ChatThread thread, CancellationToken cancellationToken = default);

    void UpdateThread(ChatThread thread);

    Task AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetMessagesByThreadAsync(
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadMessagesAsync(
        Guid threadId,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}
