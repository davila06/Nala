using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Incentives;
using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;

public sealed class UpdateLostPetStatusCommandHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    ISightingRepository sightingRepository,
    IReverseGeocodingService reverseGeocodingService,
    INotificationDispatcher notificationDispatcher,
    IContributorScoreRepository contributorScoreRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateLostPetStatusCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        UpdateLostPetStatusCommand request, CancellationToken cancellationToken)
    {
        var report = await lostPetRepository.GetByIdAsync(request.LostPetEventId, cancellationToken);

        if (report is null)
            return Result.Failure<bool>("Lost pet report not found.");

        if (report.OwnerId != request.RequestingUserId)
            return Result.Failure<bool>("Access denied.");

        Result<bool> resolveResult;
        if (request.NewStatus == LostPetStatus.Reunited)
        {
            var reunionLat = report.LastSeenLat;
            var reunionLng = report.LastSeenLng;

            if (request.ConfirmedSightingId.HasValue)
            {
                var sighting = await sightingRepository
                    .GetByIdAsync(request.ConfirmedSightingId.Value, cancellationToken);

                if (sighting is null)
                    return Result.Failure<bool>("Confirmed sighting not found.");

                if (sighting.PetId != report.PetId ||
                    (sighting.LostPetEventId.HasValue && sighting.LostPetEventId.Value != report.Id))
                {
                    return Result.Failure<bool>("Confirmed sighting does not belong to this report.");
                }

                reunionLat = sighting.Lat;
                reunionLng = sighting.Lng;
            }

            string? cantonName = null;
            if (reunionLat.HasValue && reunionLng.HasValue)
            {
                cantonName = await reverseGeocodingService
                    .ResolveCantonAsync(reunionLat.Value, reunionLng.Value, cancellationToken);
            }

            resolveResult = report.ResolveAsReunited(
                DateTimeOffset.UtcNow,
                reunionLat,
                reunionLng,
                cantonName);
        }
        else
        {
            resolveResult = report.Resolve(request.NewStatus);
        }

        if (resolveResult.IsFailure)
            return resolveResult;

        lostPetRepository.Update(report);

        // Keep Pet.Status in sync
        var pet = await petRepository.GetByIdAsync(report.PetId, cancellationToken);
        if (pet is not null)
        {
            switch (request.NewStatus)
            {
                case LostPetStatus.Reunited:
                    pet.MarkAsReunited();
                    break;
                case LostPetStatus.Cancelled:
                    pet.MarkAsActive();
                    break;
            }
            petRepository.Update(pet);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch reunited notification after DB commit
        if (request.NewStatus == LostPetStatus.Reunited && pet is not null)
        {
            var owner = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
            if (owner is not null)
            {
                await notificationDispatcher.DispatchPetReunitedAsync(
                    owner.Id,
                    owner.Email,
                    owner.Name,
                    pet.Name,
                    report.Id.ToString(),
                    cancellationToken);

                // ── Award contributor points ──────────────────────────────────────
                var existingScore = await contributorScoreRepository
                    .GetByUserIdAsync(owner.Id, cancellationToken);

                if (existingScore is null)
                {
                    var newScore = ContributorScore.Create(owner.Id, owner.Name);
                    newScore.RecordReunification(owner.Name);
                    await contributorScoreRepository.AddAsync(newScore, cancellationToken);
                }
                else
                {
                    existingScore.RecordReunification(owner.Name);
                    contributorScoreRepository.Update(existingScore);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return Result.Success(true);
    }
}
