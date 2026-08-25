namespace PawTrack.Application.Common.Settings;

public sealed class AnimalPhotoValidationSettings
{
    /// <summary>Minimum confidence score (0–1) for an animal tag to count as a match. Default: 0.60.</summary>
    public float ConfidenceThreshold { get; set; } = 0.60f;

    /// <summary>When true, photos that don't contain a detectable animal are rejected. Default: true.</summary>
    public bool EnforceOnSightings { get; set; } = true;
}
