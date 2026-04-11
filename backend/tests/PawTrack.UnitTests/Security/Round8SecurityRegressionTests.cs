using FluentAssertions;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-8 security regression tests — Domain and Application layer.
/// Controller-layer changes (RequestSizeLimit, EnableRateLimiting) are verified by
/// inspection at build time; the integration test suite covers runtime behaviour.
/// </summary>
public sealed class Round8SecurityRegressionTests
{
    // ── Fix 4: QrScanEvent — UserAgent truncation boundary is 512 chars ──────────

    [Fact]
    public void QrScanEvent_Create_StoresUserAgentExactlyAsReceived()
    {
        // The controller (PublicController.ResolveUserAgent) truncates to 512 chars
        // before passing it to this factory. The domain entity must NOT perform its
        // own silent truncation — responsibility is clearly at the app boundary.
        var exactly512 = new string('A', 512);
        var evt = QrScanEvent.Create(
            Guid.NewGuid(),
            scannedByUserId: null,
            ipAddress: null,
            userAgent: exactly512,
            countryCode: null,
            cityName: null,
            scannedAt: DateTimeOffset.UtcNow);

        evt.UserAgent.Should().HaveLength(512,
            "domain entity must not alter the value — truncation is the controller's responsibility");
    }

    [Fact]
    public void QrScanEvent_Create_NullUserAgent_StoredAsNull()
    {
        var evt = QrScanEvent.Create(
            Guid.NewGuid(),
            scannedByUserId: null,
            ipAddress: null,
            userAgent: null,
            countryCode: null,
            cityName: null,
            scannedAt: DateTimeOffset.UtcNow);

        evt.UserAgent.Should().BeNull();
    }

    [Fact]
    public void QrScanEvent_Create_WhitespaceUserAgent_StoredAsNull()
    {
        // Whitespace-only User-Agent is treated the same as absent.
        var evt = QrScanEvent.Create(
            Guid.NewGuid(),
            scannedByUserId: null,
            ipAddress: null,
            userAgent: "   ",
            countryCode: null,
            cityName: null,
            scannedAt: DateTimeOffset.UtcNow);

        evt.UserAgent.Should().BeNull();
    }
}
