using Microsoft.AspNetCore.Http;
using PawTrack.API.Middleware;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// R103 — Magic bytes validation for photo uploads (SightingsController).
/// R107 — X-XSS-Protection: 0 header added to SecurityHeadersMiddleware.
/// </summary>
public sealed class Round103And107SecurityRegressionTests
{
    // ── R107: X-XSS-Protection ───────────────────────────────────────────────

    private static async Task<IHeaderDictionary> InvokeMiddlewareAsync()
    {
        var context = new DefaultHttpContext();
        var env = new StubEnv107();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, env);
        await middleware.InvokeAsync(context);
        return context.Response.Headers;
    }

    [Fact]
    public async Task R107_SecurityHeaders_Include_XXssProtection_Zero()
    {
        var headers = await InvokeMiddlewareAsync();
        Assert.Equal("0", headers["X-XSS-Protection"].ToString());
    }

    // ── R103: Magic bytes helper ─────────────────────────────────────────────

    [Fact]
    public void R103_JpegMagicBytes_IsRecognizedAsImage()
    {
        // JPEG magic: FF D8 FF
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        using var stream = new MemoryStream(jpegBytes);
        Assert.True(ImageMagicBytesValidator.IsValidImage(stream, "image/jpeg"));
    }

    [Fact]
    public void R103_PngMagicBytes_IsRecognizedAsImage()
    {
        // PNG magic: 89 50 4E 47 0D 0A 1A 0A
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(pngBytes);
        Assert.True(ImageMagicBytesValidator.IsValidImage(stream, "image/png"));
    }

    [Fact]
    public void R103_WebpMagicBytes_IsRecognizedAsImage()
    {
        // WebP: RIFF????WEBP where ???? is file size
        var webpBytes = new byte[]
        {
            0x52, 0x49, 0x46, 0x46, // RIFF
            0x00, 0x00, 0x00, 0x00, // size (irrelevant)
            0x57, 0x45, 0x42, 0x50  // WEBP
        };
        using var stream = new MemoryStream(webpBytes);
        Assert.True(ImageMagicBytesValidator.IsValidImage(stream, "image/webp"));
    }

    [Fact]
    public void R103_ExeWithJpegContentType_IsRejected()
    {
        // MZ header (exe/PE) with jpeg content-type
        var exeBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(exeBytes);
        Assert.False(ImageMagicBytesValidator.IsValidImage(stream, "image/jpeg"));
    }

    [Fact]
    public void R103_PdfWithPngContentType_IsRejected()
    {
        // PDF header (%PDF-) with png content-type
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D };
        using var stream = new MemoryStream(pdfBytes);
        Assert.False(ImageMagicBytesValidator.IsValidImage(stream, "image/png"));
    }

    [Fact]
    public void R103_EmptyStream_IsRejected()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        Assert.False(ImageMagicBytesValidator.IsValidImage(stream, "image/jpeg"));
    }

    [Fact]
    public void R103_StreamPositionIsResetAfterValidation()
    {
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        using var stream = new MemoryStream(jpegBytes);
        _ = ImageMagicBytesValidator.IsValidImage(stream, "image/jpeg");
        // Stream must be rewound so the caller can still read the full photo
        Assert.Equal(0, stream.Position);
    }
}

// ── Stubs ─────────────────────────────────────────────────────────────────────

file sealed class StubEnv107 : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Production";
    public string ApplicationName { get; set; } = "PawTrack.API";
    public string WebRootPath { get; set; } = string.Empty;
    public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
    public string ContentRootPath { get; set; } = string.Empty;
    public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
        new Microsoft.Extensions.FileProviders.NullFileProvider();
}

