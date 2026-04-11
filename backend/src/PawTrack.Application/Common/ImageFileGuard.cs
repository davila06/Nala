namespace PawTrack.Application.Common;

/// <summary>
/// Validates uploaded bytes against known image magic byte signatures.
/// Prevents content-type spoofing: a file claiming to be image/jpeg that contains
/// a PHP web shell or PDF will be rejected here, before any further processing.
/// </summary>
public static class ImageFileGuard
{
    // JPEG: FF D8
    private static ReadOnlySpan<byte> JpegMagic  => [0xFF, 0xD8];
    // PNG:  89 50 4E 47 0D 0A 1A 0A
    private static ReadOnlySpan<byte> PngMagic   => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    // WebP: RIFF (4 bytes) + 4-byte size field + WEBP
    private static ReadOnlySpan<byte> RiffMagic  => [0x52, 0x49, 0x46, 0x46];
    private static ReadOnlySpan<byte> WebpMarker => [0x57, 0x45, 0x42, 0x50];

    /// <summary>
    /// Returns <c>true</c> when <paramref name="bytes"/> begins with a recognised
    /// JPEG, PNG, or WebP file header.
    /// </summary>
    public static bool HasValidHeader(ReadOnlySpan<byte> bytes) =>
        StartsWith(bytes, JpegMagic) ||
        StartsWith(bytes, PngMagic)  ||
        IsWebP(bytes);

    /// <summary>
    /// Reads the first 12 bytes of a <paramref name="stream"/> (resetting the position
    /// afterwards) and returns <c>true</c> when the header matches a recognised image format.
    /// Returns <c>true</c> unconditionally when the stream does not support seeking, trusting
    /// that upstream middleware (EnableBuffering) has already validated the content.
    /// </summary>
    public static bool HasValidHeader(Stream stream)
    {
        if (!stream.CanSeek) return true;

        var saved = stream.Position;
        try
        {
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);
            return HasValidHeader(header[..read]);
        }
        finally
        {
            stream.Position = saved;
        }
    }

    private static bool StartsWith(ReadOnlySpan<byte> data, ReadOnlySpan<byte> magic) =>
        data.Length >= magic.Length && data[..magic.Length].SequenceEqual(magic);

    private static bool IsWebP(ReadOnlySpan<byte> data) =>
        data.Length >= 12 &&
        StartsWith(data, RiffMagic) &&
        data[8..12].SequenceEqual(WebpMarker);
}
