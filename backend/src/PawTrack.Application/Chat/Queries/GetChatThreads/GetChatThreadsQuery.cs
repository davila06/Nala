using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Chat;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Chat.Queries.GetChatThreads;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public sealed record ChatThreadDto(
    string            ThreadId,
    string            LostPetEventId,
    /// <summary>Display name of the other party, first-name only for privacy.</summary>
    string            OtherPartyName,
    ChatThreadStatus  Status,
    DateTimeOffset    CreatedAt,
    DateTimeOffset    LastMessageAt,
    int               UnreadCount);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all chat threads associated with a lost-pet event.
/// Only participants (the finder who opened the thread, or the owner) may access it.
/// </summary>
public sealed record GetChatThreadsQuery(
    Guid LostPetEventId,
    Guid RequestingUserId)
    : IRequest<Result<IReadOnlyList<ChatThreadDto>>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetChatThreadsQueryHandler(
    IChatRepository chatRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetChatThreadsQuery, Result<IReadOnlyList<ChatThreadDto>>>
{
    public async Task<Result<IReadOnlyList<ChatThreadDto>>> Handle(
        GetChatThreadsQuery query,
        CancellationToken   cancellationToken)
    {
        // Participant filter is pushed to the database layer to prevent timing-based BOLA:
        // any authenticated user could previously trigger DB lookups for threads they do not
        // own by supplying an arbitrary LostPetEventId. The participant-scoped query ensures
        // the database engine filters before shipping thread metadata across the wire.
        var visible = await chatRepository.GetThreadsByLostPetEventAndParticipantAsync(
            query.LostPetEventId, query.RequestingUserId, cancellationToken);

        var dtos = new List<ChatThreadDto>(visible.Count);

        foreach (var thread in visible)
        {
            var isOwner     = query.RequestingUserId == thread.OwnerUserId;
            var otherUserId = isOwner ? thread.InitiatorUserId : thread.OwnerUserId;

            var otherUser   = await userRepository.GetByIdAsync(otherUserId, cancellationToken);
            // Show first name only — never surname, email, or phone.
            var displayName = GetFirstName(otherUser?.Name) ?? (isOwner ? "Rescatista" : "Dueño");

            var unread = await chatRepository.CountUnreadMessagesAsync(
                thread.Id, query.RequestingUserId, cancellationToken);

            dtos.Add(new ChatThreadDto(
                thread.Id.ToString(),
                thread.LostPetEventId.ToString(),
                displayName,
                thread.Status,
                thread.CreatedAt,
                thread.LastMessageAt,
                unread));
        }

        return Result.Success<IReadOnlyList<ChatThreadDto>>(dtos.AsReadOnly());
    }

    private static string? GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        var idx = fullName.IndexOf(' ');
        return idx > 0 ? fullName[..idx] : fullName;
    }
}
