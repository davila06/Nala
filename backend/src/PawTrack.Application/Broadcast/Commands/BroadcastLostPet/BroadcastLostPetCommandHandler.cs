using MediatR;
using PawTrack.Application.Broadcast.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Broadcast;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.Broadcast.Commands.BroadcastLostPet;

public sealed class BroadcastLostPetCommandHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    IMultichannelBroadcastService broadcastService,
    ISubscriptionService subscriptionService,
    IClinicRepository clinicRepository,
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

        // Restrict WhatsApp/Telegram/Facebook to Plus+ subscribers
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(lostEvent.OwnerId, cancellationToken);

        // Fetch featured clinics near lost location for Plus-plan broadcast footer
        IReadOnlyList<NearbyClinicRef>? nearbyClinics = null;
        if (isPlus && lostEvent.LastSeenLat.HasValue && lostEvent.LastSeenLng.HasValue)
        {
            var clinics = await clinicRepository.GetFeaturedNearAsync(
                (double)lostEvent.LastSeenLat.Value,
                (double)lostEvent.LastSeenLng.Value,
                radiusKm: 15,
                cancellationToken);

            if (clinics.Count > 0)
                nearbyClinics = clinics
                    .Take(3)
                    .Select(c => new NearbyClinicRef(c.Name, c.PhoneNumber, c.Address))
                    .ToList()
                    .AsReadOnly();
        }

        var context = new BroadcastMessageContext(
            LostPetEventId: lostEvent.Id,
            PetName: pet.Name,
            PetSpecies: pet.Species.ToString(),
            PetBreed: pet.Breed,
            OwnerEmail: owner.Email,
            OwnerContactPhone: lostEvent.ContactPhone,
            OwnerContactName: lostEvent.ContactName ?? owner.Name,
            PetProfileUrl: petProfileUrl,
            TrackingUrl: trackingUrl,
            RecentPhotoUrl: lostEvent.RecentPhotoUrl,
            LastSeenAt: lostEvent.LastSeenAt,
            LastSeenDescription: lostEvent.Description,
            RestrictToPaidChannels: !isPlus,
            NearbyFeaturedClinics: nearbyClinics);

        // ── Fan out ───────────────────────────────────────────────────────────
        var results = await broadcastService.BroadcastAsync(context, cancellationToken);

        return Result.Success<IReadOnlyList<BroadcastAttemptDto>>(results);
    }
}
