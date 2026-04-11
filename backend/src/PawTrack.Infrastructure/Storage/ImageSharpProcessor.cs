using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Storage;

public sealed class ImageSharpProcessor : IImageProcessor
{
    public async Task<byte[]> ResizeAsync(
        byte[] source,
        int maxDimension = 800,
        CancellationToken cancellationToken = default)
    {
        using var image = Image.Load(source);

        // Only downscale — never upscale
        if (image.Width > maxDimension || image.Height > maxDimension)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxDimension, maxDimension),
            }));
        }

        using var output = new MemoryStream();
        await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 }, cancellationToken);
        return output.ToArray();
    }
}
