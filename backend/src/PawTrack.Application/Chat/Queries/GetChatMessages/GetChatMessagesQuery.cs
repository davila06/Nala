using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Chat.Queries.GetChatMessages;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ChatMessageDto(
    string MessageId,
    /// <summary><c>true</c> when the requesting user sent this message.</summary>
    bool IsFromMe,
    string Body,
    DateTimeOffset SentAt,
    bool IsReadByRecipient);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a page of messages in a chat thread for a participant.
/// Marks the returned unread messages as read so the sender sees delivery confirmation.
/// Pass <paramref name="BeforeMessageId"/> to page backward (cursor pagination).
/// </summary>
public sealed record GetChatMessagesQuery(
    Guid ThreadId,
    Guid RequestingUserId,
    Guid? BeforeMessageId = null,
    int PageSize = 50)
    : IRequest<Result<IReadOnlyList<ChatMessageDto>>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetChatMessagesQueryHandler(
    IChatRepository chatRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetChatMessagesQuery, Result<IReadOnlyList<ChatMessageDto>>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<IReadOnlyList<ChatMessageDto>>> Handle(
        GetChatMessagesQuery query,
        CancellationToken cancellationToken)
    {
        var thread = await chatRepository.GetThreadByIdAsync(query.ThreadId, cancellationToken);
        if (thread is null)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>("Hilo de conversación no encontrado.");

        var isParticipant = query.RequestingUserId == thread.InitiatorUserId
                            || query.RequestingUserId == thread.OwnerUserId;
        if (!isParticipant)
            return Result.Failure<IReadOnlyList<ChatMessageDto>>("Acceso denegado.");

        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var messages = await chatRepository.GetMessagesByThreadAsync(
            query.ThreadId, query.BeforeMessageId, pageSize, cancellationToken);

        // Bulk-mark incoming messages on this page as read via a single SQL UPDATE.
        // ExecuteUpdateAsync bypasses change tracking, so no UoW.SaveChanges needed.
        var unreadIds = messages
            .Where(m => m.SenderUserId != query.RequestingUserId && !m.IsReadByRecipient)
            .Select(m => m.Id)
            .ToList();

        if (unreadIds.Count > 0)
            await chatRepository.MarkMessagesAsReadAsync(unreadIds, cancellationToken);

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
