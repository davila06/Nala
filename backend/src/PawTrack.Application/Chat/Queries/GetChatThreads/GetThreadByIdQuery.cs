using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Chat.Queries.GetChatThreads;

/// <summary>
/// Returns metadata for a single chat thread by its ID.
/// Both participants (owner and finder) may access their own thread.
/// </summary>
public sealed record GetThreadByIdQuery(
    Guid ThreadId,
    Guid RequestingUserId)
    : IRequest<Result<ChatThreadDto>>;

public sealed class GetThreadByIdQueryHandler(
    IChatRepository chatRepository,
    IUserRepository userRepository)
    : IRequestHandler<GetThreadByIdQuery, Result<ChatThreadDto>>
{
    public async Task<Result<ChatThreadDto>> Handle(
        GetThreadByIdQuery query, CancellationToken cancellationToken)
    {
        var thread = await chatRepository.GetThreadByIdAsync(query.ThreadId, cancellationToken);
        if (thread is null)
            return Result.Failure<ChatThreadDto>("Thread not found.");

        var isParticipant = query.RequestingUserId == thread.OwnerUserId
                         || query.RequestingUserId == thread.InitiatorUserId;
        if (!isParticipant)
            return Result.Failure<ChatThreadDto>("Access denied.");

        var isOwner = query.RequestingUserId == thread.OwnerUserId;
        var otherUserId = isOwner ? thread.InitiatorUserId : thread.OwnerUserId;
        var otherUser = await userRepository.GetByIdAsync(otherUserId, cancellationToken);

        var displayName = GetFirstName(otherUser?.Name) ?? (isOwner ? "Rescatista" : "Dueño");

        var unread = await chatRepository.CountUnreadMessagesAsync(
            thread.Id, query.RequestingUserId, cancellationToken);

        return Result.Success(new ChatThreadDto(
            thread.Id.ToString(),
            thread.LostPetEventId.ToString(),
            displayName,
            thread.Status,
            thread.CreatedAt,
            thread.LastMessageAt,
            unread));
    }

    private static string? GetFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;
        var idx = fullName.IndexOf(' ');
        return idx > 0 ? fullName[..idx] : fullName;
    }
}
