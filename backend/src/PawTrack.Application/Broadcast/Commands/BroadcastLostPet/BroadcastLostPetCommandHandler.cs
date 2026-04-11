using MediatR;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Broadcast.Commands.BroadcastLostPet;

public sealed class BroadcastLostPetCommandHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    IMultichannelBroadcastService broadcastService,
    ITrackingLinkService trackingLinkService,
    IPublicAppUrlProvider publicAppUrlProvider)
    : IRequestHandler<BroadcastLostPetCommand, Result<IReadOnlyList<BroadcastAttemptDto>>>
{
    public async Task<Result<IReadOnlyList<BroadcastAttemptDto>>> Handle(
        BroadcastLostPetCommand request,
        CancellationToken cancellationToken)
    {
        // ── Authorization ─────────────────────────────────────────────────────
        var lostEvent = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);
        if (lostEvent is null)
            return Result.Failure<IReadOnlyList<BroadcastAttemptDto>>("Lost pet report not found.");

        if (lostEvent.OwnerId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<BroadcastAttemptDto>>("Access denied.");

        if (lostEvent.Status != LostPetStatus.Active)
            return Result.Failure<IReadOnlyList<BroadcastAttemptDto>>("Only active reports can be broadcast.");

        // ── Resolve related data ──────────────────────────────────────────────
        var pet = await petRepository.GetByIdAsync(lostEvent.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<IReadOnlyList<BroadcastAttemptDto>>("Pet not found.");

        var owner = await userRepository.GetByIdAsync(lostEvent.OwnerId, cancellationToken);
        if (owner is null)
            return Result.Failure<IReadOnlyList<BroadcastAttemptDto>>("Owner not found.");

        // ── Build broadcast context ───────────────────────────────────────────
        var baseUrl = publicAppUrlProvider.GetBaseUrl();
        var petProfileUrl = $"{baseUrl}/p/{pet.Id}";
        var trackingUrl = trackingLinkService.Generate(lostEvent.Id, "multicast");

        var context = new BroadcastMessageContext(
            LostPetEventId: lostEvent.Id,
            PetName: pet.Name,
            PetSpecies: pet.Species.ToString(),
            PetBreed: pet.Breed,
            OwnerEmail: owner.Email,
            OwnerContactName: lostEvent.ContactName ?? owner.Name,
            PetProfileUrl: petProfileUrl,
            TrackingUrl: trackingUrl,
            RecentPhotoUrl: lostEvent.RecentPhotoUrl,
            LastSeenAt: lostEvent.LastSeenAt,
            LastSeenDescription: lostEvent.Description);

        // ── Fan out ───────────────────────────────────────────────────────────
        var results = await broadcastService.BroadcastAsync(context, cancellationToken);

        return Result.Success<IReadOnlyList<BroadcastAttemptDto>>(results);
    }
}
