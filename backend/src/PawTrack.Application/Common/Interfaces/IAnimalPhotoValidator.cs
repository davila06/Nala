namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Validates that an uploaded photo contains a recognizable animal before persisting.
/// Implementations must be fail-open: an unavailable service returns <c>IsAnimalDetected = true</c>
/// so that a Vision API outage never blocks legitimate sighting reports.
/// </summary>
public interface IAnimalPhotoValidator
{
    Task<AnimalPhotoValidationResult> ValidateAsync(
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken = default);
}

/// <param name="IsAnimalDetected">True when an animal tag meets the confidence threshold, or when the service is unavailable (fail-open).</param>
/// <param name="Confidence">Highest confidence score among matched animal tags. 0 when service unavailable.</param>
/// <param name="DetectedTags">All animal-relevant tags returned by the service.</param>
/// <param name="ServiceAvailable">False when the Vision API was unreachable or not configured.</param>
public sealed record AnimalPhotoValidationResult(
    bool IsAnimalDetected,
    float Confidence,
    IReadOnlyList<string> DetectedTags,
    bool ServiceAvailable)
{
    /// <summary>Fail-open sentinel — used when Vision API is unreachable or not configured.</summary>
    public static AnimalPhotoValidationResult ServiceUnavailable =>
        new(IsAnimalDetected: true, Confidence: 0f, DetectedTags: [], ServiceAvailable: false);
}
