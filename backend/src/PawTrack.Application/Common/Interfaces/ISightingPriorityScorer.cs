using PawTrack.Application.Sightings.Scoring;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Common.Interfaces;

public interface ISightingPriorityScorer
{
    SightingPriority Score(
        Pet pet,
        LostPetEvent? lostPetEvent,
        Sighting sighting,
        DateTimeOffset? referenceTime = null);
}