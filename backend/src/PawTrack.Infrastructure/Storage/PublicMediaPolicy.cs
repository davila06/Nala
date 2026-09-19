namespace PawTrack.Infrastructure.Storage;

public static class PublicMediaPolicy
{
    private static readonly HashSet<string> PublicContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "pet-photos",
        "sighting-photos",
        "found-pet-photos",
        "lost-pet-photos",
        "adoption-photos",
        "billboard-images",
        "clinic-logos",
        "store-product-images",
    };

    public static bool IsPublicContainer(string containerName) =>
        PublicContainers.Contains(containerName);

    public static string BuildPublicUrl(string baseUrl, string containerName, string blobName)
    {
        if (!IsPublicContainer(containerName))
            throw new ArgumentException("Container is not approved for public delivery.", nameof(containerName));

        var segments = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (blobName.StartsWith('/') || segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            throw new ArgumentException("Blob path is invalid.", nameof(blobName));

        var encodedPath = string.Join('/', segments.Select(Uri.EscapeDataString));
        return $"{baseUrl.TrimEnd('/')}/api/public/media/{containerName}/{encodedPath}";
    }
}
