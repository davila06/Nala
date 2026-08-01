using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Subscriptions.Commands.ActivateSubscription;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace PawTrack.API.Controllers;

/// <summary>
/// Receives payment notifications from payment processors (BAC Credomatic, FlexiPago, etc.).
/// The caller must include an HMAC-SHA256 signature in the X-Webhook-Signature header.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(ISender sender, IConfiguration configuration, ILogger<WebhooksController> logger) : ControllerBase
{
    // ── POST /api/webhooks/sinpe ──────────────────────────────────────────────
    [HttpPost("sinpe")]
    [EnableRateLimiting("public-api")] // 30/min — payment processors should not call more than once per reference
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SinpePayment(
        [FromBody] SinpePaymentNotification notification,
        CancellationToken cancellationToken)
    {
        // Validate HMAC-SHA256 signature from payment processor
        if (!ValidateSignature(notification))
        {
            logger.LogWarning("Webhook signature validation failed for reference {Reference}.", notification.Reference);
            return Unauthorized(new ProblemDetails { Detail = "Invalid webhook signature." });
        }

        // Try to activate a subscription matching the reference
        var subResult = await sender.Send(
            new ActivateSubscriptionCommand(notification.Reference),
            cancellationToken);

        if (subResult.IsSuccess && subResult.Value is not null)
            return Ok(new { activated = "subscription", id = subResult.Value.Id });

        // If no subscription matched, try to activate a bounty deposit
        var bountyResult = await sender.Send(
            new ConfirmBountyDepositCommand(notification.Reference),
            cancellationToken);

        if (bountyResult.IsSuccess && bountyResult.Value is not null)
            return Ok(new { activated = "bounty", id = bountyResult.Value.Id });

        // Reference not found — acknowledge to avoid retries but log discrepancy
        return Ok(new { message = "Reference not found; acknowledged." });
    }

    private bool ValidateSignature(SinpePaymentNotification notification)
    {
        var secret = configuration["Webhooks:SinpeSecret"];
        if (string.IsNullOrEmpty(secret)) return false; // misconfigured — reject

        if (!Request.Headers.TryGetValue("X-Webhook-Signature", out var receivedSig))
            return false;

        var payload = $"{notification.Reference}:{notification.AmountCrc}";
        var expected = ComputeHmac(secret, payload);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(receivedSig.ToString()),
            Encoding.UTF8.GetBytes(expected));
    }

    private static string ComputeHmac(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return Convert.ToHexString(HMACSHA256.HashData(keyBytes, dataBytes)).ToLowerInvariant();
    }
}

public sealed record SinpePaymentNotification(
    [property: JsonPropertyName("reference")] string Reference,
    [property: JsonPropertyName("amount_crc")] decimal AmountCrc,
    [property: JsonPropertyName("sender_name")] string? SenderName,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);
