using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PawTrack.Infrastructure.AI;

/// <summary>
/// Vectorises images using the Azure Computer Vision 4.0 Image Retrieval API.
/// Produces 1024-dimensional float embeddings suitable for cosine-similarity matching.
/// <para>
/// Configuration keys: <c>Azure:Vision:Endpoint</c> and <c>Azure:Vision:Key</c>.
/// When either key is absent the service returns <c>null</c> gracefully.
/// </para>
/// </summary>
public sealed class AzureVisionEmbeddingService(
    IHttpClientFactory httpClientFactory,
    IConfiguration     configuration,
    ILogger<AzureVisionEmbeddingService> logger)
    : IImageEmbeddingService
{
    private const string ApiVersion     = "2024-02-01";
    private const string HttpClientName = "AzureVision";

    public async Task<float[]?> VectorizeStreamAsync(
        Stream     imageStream,
        string     contentType,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetConfig(out var endpoint, out var key)) return null;

        using var client  = httpClientFactory.CreateClient(HttpClientName);
        using var content = new StreamContent(imageStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(endpoint));
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        request.Content = content;

        return await SendAsync(client, request, cancellationToken);
    }

    public async Task<float[]?> VectorizeUrlAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetConfig(out var endpoint, out var key)) return null;

        using var client  = httpClientFactory.CreateClient(HttpClientName);
        using var content = JsonContent.Create(new { url = imageUrl });

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl(endpoint));
        request.Headers.Add("Ocp-Apim-Subscription-Key", key);
        request.Content = content;

        return await SendAsync(client, request, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetConfig(out string endpoint, out string key)
    {
        endpoint = configuration["Azure:Vision:Endpoint"] ?? string.Empty;
        key      = configuration["Azure:Vision:Key"]      ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(key))
            return true;

        logger.LogDebug("Azure Vision is not configured — visual matching unavailable");
        return false;
    }

    private static string BuildUrl(string endpoint) =>
        $"{endpoint.TrimEnd('/')}/computervision/retrieval:vectorizeImage?api-version={ApiVersion}";

    private async Task<float[]?> SendAsync(
        HttpClient        client,
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Azure Vision returned {Status} for vectorisation request",
                    response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<VectorResponse>(
                cancellationToken: cancellationToken);

            return result?.Vector;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Azure Vision embedding request failed");
            return null;
        }
    }

    // ── Private response model ────────────────────────────────────────────────

    private sealed record VectorResponse(
        [property: JsonPropertyName("vector")] float[] Vector);
}
