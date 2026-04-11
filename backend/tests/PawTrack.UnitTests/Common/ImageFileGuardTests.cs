using FluentAssertions;
using PawTrack.Application.Common;

namespace PawTrack.UnitTests.Common;

public sealed class ImageFileGuardTests
{
    // ── byte[] overload ───────────────────────────────────────────────────────

    [Fact]
    public void HasValidHeader_JpegBytes_ReturnsTrue()
    {
        byte[] bytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        ImageFileGuard.HasValidHeader((ReadOnlySpan<byte>)bytes).Should().BeTrue();
    }

    [Fact]
    public void HasValidHeader_PngBytes_ReturnsTrue()
    {
        byte[] bytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        ImageFileGuard.HasValidHeader((ReadOnlySpan<byte>)bytes).Should().BeTrue();
    }

    [Fact]
    public void HasValidHeader_WebPBytes_ReturnsTrue()
    {
        // RIFF....WEBP
        byte[] bytes = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
        ImageFileGuard.HasValidHeader((ReadOnlySpan<byte>)bytes).Should().BeTrue();
    }

    [Fact]
    public void HasValidHeader_PdfBytes_ReturnsFalse()
    {
        byte[] bytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31]; // %PDF-1
        ImageFileGuard.HasValidHeader((ReadOnlySpan<byte>)bytes).Should().BeFalse();
    }

    [Fact]
    public void HasValidHeader_EmptySpan_ReturnsFalse()
    {
        ImageFileGuard.HasValidHeader(ReadOnlySpan<byte>.Empty).Should().BeFalse();
    }

    // ── Stream overload ───────────────────────────────────────────────────────

    [Fact]
    public void HasValidHeader_SeekableStreamWithJpegBytes_ReturnsTrue()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        using var stream = new MemoryStream(jpeg);

        ImageFileGuard.HasValidHeader(stream).Should().BeTrue();
    }

    [Fact]
    public void HasValidHeader_SeekableStream_ResetsPositionAfterCheck()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        using var stream = new MemoryStream(png);
        stream.Position = 0;

        ImageFileGuard.HasValidHeader(stream);

        stream.Position.Should().Be(0, "stream position must be restored after the magic-byte check");
    }

    [Fact]
    public void HasValidHeader_SeekableStreamWithPdfBytes_ReturnsFalse()
    {
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31];
        using var stream = new MemoryStream(pdf);

        ImageFileGuard.HasValidHeader(stream).Should().BeFalse();
    }

    [Fact]
    public void HasValidHeader_NonSeekableStream_ReturnsTrueWithoutReading()
    {
        // Non-seekable streams cannot be inspected; caller trusts prior validation.
        using var inner = new NonSeekableStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });
        ImageFileGuard.HasValidHeader(inner).Should().BeTrue("non-seekable streams are trusted by default");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private sealed class NonSeekableStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);
        public override bool CanSeek  => false;
        public override bool CanRead  => true;
        public override bool CanWrite => false;
        public override long Length   => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override int  Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
