using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.LostPets.Queries.IsSearchParticipant;

/// <summary>
/// Returns <c>true</c> when <paramref name="UserId"/> is a legitimate participant
/// of the search associated with <paramref name="LostEventId"/>.
///
/// <para>
/// A user is a <b>participant</b> when at least one of the following conditions holds:
///   <list type="bullet">
///     <item>They are the owner of the lost-pet event (<c>LostPetEvent.OwnerId</c>).</item>
///     <item>They have an active chat thread for the event as the initiating finder
///           (<c>IChatRepository.ThreadExistsAsync</c>).</item>
///   </list>
/// </para>
///
/// <para>
/// This query is used as a gate in <c>SearchCoordinationHub.JoinSearch</c> to
/// prevent any arbitrary authenticated user from joining a real-time GPS
/// coordinate broadcast group.
/// </para>
/// </summary>
public sealed record IsSearchParticipantQuery(Guid LostEventId, Guid UserId)
    : IRequest<Result<bool>>;

public sealed class IsSearchParticipantQueryHandler(
    ILostPetRepository lostPetRepository,
    IChatRepository chatRepository)
    : IRequestHandler<IsSearchParticipantQuery, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        IsSearchParticipantQuery request,
        CancellationToken cancellationToken)
    {
        var evt = await lostPetRepository.GetByIdAsync(request.LostEventId, cancellationToken);
        if (evt is null)
            return Result.Success(false); // event doesn't exist — silently deny; no info leak

        // Owner always has access to their own search group.
        if (evt.OwnerId == request.UserId)
            return Result.Success(true);

        // Finders and rescuers identify themselves by opening a chat thread for the event.
        var hasChatThread = await chatRepository.ThreadExistsAsync(
            request.LostEventId, request.UserId, cancellationToken);

        return Result.Success(hasChatThread);
    }
}
