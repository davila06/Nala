namespace PawTrack.Application.Common;

public static class BlobHelper
{
    /// <summary>Strips dangerous characters from user-supplied file names before use in blob paths.</summary>
    public static string SanitizeFileName(string? fileName, string fallback = "photo.jpg")
    {
        if (string.IsNullOrWhiteSpace(fileName)) return fallback;
        var clean = new string(fileName
            .Where(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_')
            .ToArray());
        return string.IsNullOrEmpty(clean) ? fallback : clean;
    }
}
