using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Chat.Queries.GetChatMessages;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ChatMessageDto(
    string         MessageId,
    /// <summary><c>true</c> when the requesting user sent this message.</summary>
    bool           IsFromMe,
    string         Body,
    DateTimeOffset SentAt,
    bool           IsReadByRecipient);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all messages in a chat thread for a participant.
/// Marks unread messages as read so the sender sees delivery confirmation.
/// </summary>
public sealed record GetChatMessagesQuery(
    Guid ThreadId,
    Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetChatMessagesQueryHandler(
    IChatRepository chatRepository,
    IUnitOfWork     unitOfWork)
    : IRequestHandler<GetChatMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
        GetChatMessagesQuery query,
        CancellationToken    cancellationToken)
    {
        var thread = await chatRepository.GetThreadByIdAsync(query.ThreadId, cancellationToken);
        if (thread is null)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>("Hilo de conversación no encontrado.");

        var isParticipant = query.RequestingUserId == thread.InitiatorUserId
                            || query.RequestingUserId == thread.OwnerUserId;
        if (!isParticipant)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>("Acceso denegado.");

        var messages = await chatRepository.GetMessagesByThreadAsync(query.ThreadId, cancellationToken);

        // Mark incoming messages as read (side-effect on GET is acceptable here for UX).
        var unread = messages
            .Where(m => m.SenderUserId != query.RequestingUserId && !m.IsReadByRecipient)
            .ToList();

        if (unread.Count > 0)
        {
            foreach (var m in unread) m.MarkAsRead();
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dtos = messages
            .Select(m => new ChatMessageDto(
                m.Id.ToString(),
                m.SenderUserId == query.RequestingUserId,
                m.Body,
                m.SentAt,
                m.IsReadByRecipient))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<ChatMessageDto>>(dtos);
    }
}
