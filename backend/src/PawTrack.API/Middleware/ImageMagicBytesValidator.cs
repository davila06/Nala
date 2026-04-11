namespace PawTrack.API.Middleware;

/// <summary>
/// Validates image uploads using magic bytes (file signatures) rather than
/// trusting the client-supplied Content-Type header.
///
/// Attack prevented: An attacker submitting a malicious file (e.g., .exe, .php)
/// with a spoofed <c>Content-Type: image/jpeg</c> header is rejected here before
/// the stream ever reaches blob storage or the embedding service.
///
/// Magic byte references:
///   JPEG  — FF D8 FF
///   PNG   — 89 50 4E 47 0D 0A 1A 0A
///   WebP  — 52 49 46 46 [4 size bytes] 57 45 42 50
/// </summary>
public static class ImageMagicBytesValidator
{
    private static readonly byte[] JpegMagic  = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic   = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    // WebP: RIFF at bytes 0-3, WEBP at bytes 8-11
    private static readonly byte[] RiffMagic  = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpMarker = [0x57, 0x45, 0x42, 0x50];

    /// <summary>
    /// Reads the first 12 bytes of <paramref name="stream"/> to detect the image
    /// format, then rewinds the stream to position 0 so the caller can read it again.
    /// Returns <c>false</c> if the bytes do not match the expected <paramref name="contentType"/>.
    /// </summary>
    public static bool IsValidImage(Stream stream, string contentType)
    {
        if (!stream.CanRead || !stream.CanSeek)
            return false;

        Span<byte> header = stackalloc byte[12];
        var originalPosition = stream.Position;

        stream.Position = 0;
        int read = stream.Read(header);
        stream.Position = 0; // always rewind

        if (read < 3)
            return false;

        return contentType switch
        {
            "image/jpeg" => StartsWith(header[..read], JpegMagic),
            "image/png"  => read >= 8 && StartsWith(header[..read], PngMagic),
            "image/webp" => read >= 12
                            && StartsWith(header[..4], RiffMagic)
                            && StartsWith(header[8..12], WebpMarker),
            _            => false,
        };
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        if (data.Length < signature.Length) return false;
        return data[..signature.Length].SequenceEqual(signature);
    }
}
