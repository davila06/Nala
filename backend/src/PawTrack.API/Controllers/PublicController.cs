using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Pets.Commands.RecordPublicQrScan;
using PawTrack.Application.Pets.Queries.GetPublicPetProfile;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

[ApiController]
[Route("api/public")]
[EnableRateLimiting("public-api")] // 30 req/min per IP — prevents QR-scan farming
public sealed class PublicController(ISender sender, ILogger<PublicController> logger) : ControllerBase
{
    // ── GET /api/public/pets/{id} ─────────────────────────────────────────────
    [HttpGet("pets/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublicPetProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPublicPetProfileQuery(id), cancellationToken);

        if (result.IsFailure)
            return NotFound(new ProblemDetails { Title = "Pet not found", Status = 404 });

        try
        {
            var command = new RecordPublicQrScanCommand(
                id,
                TryGetAuthenticatedUserId(),
                ResolveClientIp(),
                ResolveUserAgent(),
                ResolveCountryCode(),
                ResolveCityName(),
                DateTimeOffset.UtcNow,
                ResolveScanLat(),
                ResolveScanLng());

            await sender.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            // Scan logging is best-effort and must never block public profile rendering.
            logger.LogWarning(ex, "QR scan audit record failed for pet {PetId}", id);
        }

        return Ok(result.Value);
    }

    private Guid? TryGetAuthenticatedUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return Guid.TryParse(claim, out var parsed) ? parsed : null;
    }

    private string? ResolveClientIp()
    {
        // After UseForwardedHeaders() middleware (configured in Program.cs with
        // trusted KnownNetworks), HttpContext.Connection.RemoteIpAddress already
        // holds the real client IP unwrapped from X-Forwarded-For.
        // We must NOT re-read the raw header here — doing so would allow any client
        // to spoof their IP by crafting the header value.
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? ResolveCountryCode()
    {
        // Accept only valid ISO 3166-1 alpha-2 codes (exactly 2 uppercase letters).
        // Rejects spoofed / oversized Cloudflare header values.
        static string? ExtractValidCode(string raw)
        {
            var v = raw.Trim();
            return v.Length == 2 && char.IsLetter(v[0]) && char.IsLetter(v[1])
                ? v.ToUpperInvariant()
                : null;
        }

        var value = Request.Headers["CF-IPCountry"].ToString();
        if (!string.IsNullOrWhiteSpace(value))
            return ExtractValidCode(value);

        value = Request.Headers["X-Country-Code"].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : ExtractValidCode(value);
    }

    private string? ResolveCityName()
    {
        // Cap to 100 chars to prevent oversized header values from being stored in the DB.
        static string? TruncateCity(string raw)
        {
            var v = raw.Trim();
            return string.IsNullOrEmpty(v) ? null : v[..Math.Min(v.Length, 100)];
        }

        var value = Request.Headers["CF-IPCity"].ToString();
        if (!string.IsNullOrWhiteSpace(value))
            return TruncateCity(value);

        value = Request.Headers["X-City"].ToString();
        return string.IsNullOrWhiteSpace(value) ? null : TruncateCity(value);
    }

    private double? ResolveScanLat()
    {
        var raw = Request.Query["scanLat"].ToString();
        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private double? ResolveScanLng()
    {
        var raw = Request.Query["scanLng"].ToString();
        return double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private string? ResolveUserAgent()
    {
        // Cap at 512 characters — the DB column limit set in QrScanEventConfiguration.
        // Without this guard, a crafted User-Agent longer than 512 chars would cause
        // an EF Core SaveChanges exception that is currently silently swallowed
        // (scan logging is best-effort). Truncating here prevents silent data loss.
        var raw = Request.Headers.UserAgent.ToString();
        return string.IsNullOrWhiteSpace(raw) ? null : raw[..Math.Min(raw.Length, 512)];
    }
}
