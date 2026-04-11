namespace PawTrack.Domain.LostPets;

/// <summary>Lifecycle state of a <see cref="SearchZone"/> during a coordinated field search.</summary>
public enum SearchZoneStatus
{
    /// <summary>Zone is available for any volunteer to claim.</summary>
    Free,

    /// <summary>Zone has been claimed by a volunteer and is actively being searched.</summary>
    Taken,

    /// <summary>Zone has been fully searched and cleared by its volunteer.</summary>
    Clear,
}
