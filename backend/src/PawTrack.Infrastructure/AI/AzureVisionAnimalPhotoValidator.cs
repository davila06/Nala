using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PawTrack.Infrastructure.AI;

/// <summary>
/// Validates that a photo contains an animal using Azure AI Vision 4.0 Image Analysis (tags feature).
/// Reuses the existing "AzureVision" HttpClient and Azure:Vision:{Endpoint,Key} configuration.
/// </summary>
public sealed class AzureVisionAnimalPhotoValidator(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IOptions<AnimalPhotoValidationSettings> options,
    ILogger<AzureVisionAnimalPhotoValidator> logger)
    : IAnimalPhotoValidator
{
    // All tags that indicate a recognisable animal is present in the image.
    private static readonly HashSet<string> AnimalTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "dog", "cat", "animal", "mammal", "canine", "feline",
        "puppy", "kitten", "bird", "rabbit", "hamster", "guinea pig",
        "turtle", "reptile", "lizard", "fish", "parrot", "cockatiel",
        "budgerigar", "ferret", "pet", "domestic animal", "wildlife",
        "veterinary", "paw", "snout", "fur", "feather",
    };

    private const string ApiVersion = "2024-02-01";

    public async Task<AnimalPhotoValidationResult> ValidateAsync(
        Stream photoStream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var endpoint = configuration["Azure:Vision:Endpoint"];
        var key = configuration["Azure:Vision:Key"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key))
        {
            logger.LogDebug("Azure Vision not configured — animal photo validation skipped (fail-open)");
            return AnimalPhotoValidationResult.ServiceUnavailable;
        }

        var url = $"{endpoint.TrimEnd('/')}/computervision/imageanalysis:analyze" +
                  $"?api-version={ApiVersion}&features=tags&language=en";

        try
        {
            using var client = httpClientFactory.CreateClient("AzureVision");
            using var content = new StreamContent(photoStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", key);
            request.Content = content;

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Azure Vision returned {Status} for animal photo validation — fail-open",
                    (int)response.StatusCode);
                return AnimalPhotoValidationResult.ServiceUnavailable;
            }

            var result = await response.Content
                .ReadFromJsonAsync<ImageAnalysisResponse>(cancellationToken: cancellationToken);

            if (result?.TagsResult?.Values is null)
                return AnimalPhotoValidationResult.ServiceUnavailable;

            var threshold = options.Value.ConfidenceThreshold;
            var matchedTags = result.TagsResult.Values
                .Where(t => AnimalTags.Contains(t.Name) && t.Confidence >= threshold)
                .ToList();

            var isAnimal = matchedTags.Count > 0;
            var topConfidence = matchedTags.Count > 0
                ? matchedTags.Max(t => t.Confidence)
                : 0f;
            var tagNames = matchedTags.Select(t => t.Name).ToList();

            logger.LogInformation(
                "Animal photo validation: detected={Detected} confidence={Confidence:F2} tags=[{Tags}]",
                isAnimal, topConfidence, string.Join(", ", tagNames));

            return new AnimalPhotoValidationResult(isAnimal, topConfidence, tagNames, ServiceAvailable: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Azure Vision animal photo validation threw — fail-open");
            return AnimalPhotoValidationResult.ServiceUnavailable;
        }
    }

    // ── Private response models ───────────────────────────────────────────────

    private sealed record ImageAnalysisResponse(
        [property: JsonPropertyName("tagsResult")] TagsResult? TagsResult);

    private sealed record TagsResult(
        [property: JsonPropertyName("values")] List<TagValue>? Values);

    private sealed record TagValue(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("confidence")] float Confidence);
}
