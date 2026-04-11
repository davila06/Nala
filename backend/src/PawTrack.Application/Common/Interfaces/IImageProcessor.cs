namespace PawTrack.Application.Common.Interfaces;

/// <summary>Resizes and encodes images before storage upload.</summary>
public interface IImageProcessor
{
    /// <summary>
    /// Resizes the image to fit within <paramref name="maxDimension"/> × <paramref name="maxDimension"/>
    /// while preserving aspect ratio. Returns JPEG bytes.
    /// </summary>
    Task<byte[]> ResizeAsync(
        byte[] source,
        int maxDimension = 800,
        CancellationToken cancellationToken = default);
}
