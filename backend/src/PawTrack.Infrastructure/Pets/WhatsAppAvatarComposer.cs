using PawTrack.Application.Common.Interfaces;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PawTrack.Infrastructure.Pets;

public sealed class WhatsAppAvatarComposer(
    IBlobStorageService blobStorage,
    IQrCodeService qrCodeService) : IWhatsAppAvatarService
{
    private const int CanvasSize = 640;
    private const int QrSize = 180;
    private const int QrBorder = 4;
    private const int Margin = 20;
    private const int BottomBandHeight = 96;

    public async Task<byte[]> BuildAvatarAsync(
        string? sourcePhotoUrl,
        string profileUrl,
        string petName,
        CancellationToken cancellationToken = default)
    {
        using var canvas = new Image<Rgba32>(CanvasSize, CanvasSize);

        await PaintBackgroundAsync(canvas, sourcePhotoUrl, cancellationToken);
        PaintBottomBand(canvas);

        var safePetName = string.IsNullOrWhiteSpace(petName) ? "tu mascota" : petName.Trim();
        PaintText(canvas, $"Perdi a {safePetName} 🐾");

        var qrBytes = qrCodeService.GeneratePng(profileUrl);
        using var qr = Image.Load<Rgba32>(qrBytes);
        qr.Mutate(ctx => ctx.Resize(QrSize, QrSize));

        var qrOuterX = CanvasSize - Margin - (QrSize + QrBorder * 2);
        var qrOuterY = CanvasSize - BottomBandHeight - Margin - (QrSize + QrBorder * 2);

        canvas.Mutate(ctx =>
        {
            ctx.Fill(Color.White, new Rectangle(qrOuterX, qrOuterY, QrSize + QrBorder * 2, QrSize + QrBorder * 2));
            ctx.DrawImage(qr, new Point(qrOuterX + QrBorder, qrOuterY + QrBorder), 1f);
        });

        using var output = new MemoryStream();
        await canvas.SaveAsPngAsync(output, cancellationToken);
        return output.ToArray();
    }

    private async Task PaintBackgroundAsync(Image<Rgba32> canvas, string? sourcePhotoUrl, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(sourcePhotoUrl))
        {
            var bytes = await blobStorage.DownloadAsync(sourcePhotoUrl, ct);
            if (bytes is { Length: > 0 })
            {
                using var source = Image.Load<Rgba32>(bytes);
                source.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(CanvasSize, CanvasSize),
                    Mode = ResizeMode.Crop,
                    Position = AnchorPositionMode.Center,
                }));

                canvas.Mutate(ctx => ctx.DrawImage(source, new Point(0, 0), 1f));
                return;
            }
        }

        canvas.Mutate(ctx =>
        {
            ctx.Fill(Color.ParseHex("#0F172A"));
            ctx.Fill(Color.ParseHex("#1E3A8A"), new EllipsePolygon(CanvasSize * 0.22f, CanvasSize * 0.25f, 180));
            ctx.Fill(Color.ParseHex("#0EA5E9"), new EllipsePolygon(CanvasSize * 0.78f, CanvasSize * 0.35f, 160));
            ctx.Fill(Color.ParseHex("#22C55E"), new EllipsePolygon(CanvasSize * 0.45f, CanvasSize * 0.72f, 210));
        });
    }

    private static void PaintBottomBand(Image<Rgba32> canvas)
    {
        canvas.Mutate(ctx =>
        {
            ctx.Fill(new Color(new Rgba32(0, 0, 0, 160)), new Rectangle(0, CanvasSize - BottomBandHeight, CanvasSize, BottomBandHeight));
        });
    }

    private static void PaintText(Image<Rgba32> canvas, string text)
    {
        var font = SystemFonts.CreateFont("Segoe UI", 36, FontStyle.Bold);
        var options = new RichTextOptions(font)
        {
            Origin = new PointF(22, CanvasSize - BottomBandHeight + 22),
        };

        canvas.Mutate(ctx =>
        {
            ctx.DrawText(options, text, Color.White);
        });
    }
}
