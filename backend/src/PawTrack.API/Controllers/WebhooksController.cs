using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Bundles;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ActivateSubscription;
using PawTrack.Application.Webhooks.Commands;
using PawTrack.Domain.Bundles;
using PawTrack.Domain.Payments;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;

namespace PawTrack.API.Controllers;

/// <summary>
/// Receives payment notifications from payment processors (BAC Credomatic, FlexiPago, etc.).
/// The caller must include an HMAC-SHA256 signature in the X-Webhook-Signature header.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class WebhooksController(
    ISender sender,
    IBundleOrderRepository bundleRepository,
    IPaymentTransactionRepository transactionRepository,
    IConfiguration configuration,
    ILogger<WebhooksController> logger) : ControllerBase
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

        // Idempotency check: prevent duplicate execution if this reference already processed
        var existingTx = await transactionRepository.GetByReferenceAsync($"SINPE-{notification.Reference}", cancellationToken);
        if (existingTx is not null && existingTx.Status == PaymentTransactionStatus.Succeeded)
        {
            logger.LogInformation("Webhook SINPE duplicate detected for reference {Reference}. Returning 200 OK.", notification.Reference);
            return Ok(new { message = "Payment already processed.", reference = notification.Reference });
        }

        // 1. Try to activate a subscription matching the reference
        var subResult = await sender.Send(
            new ActivateSubscriptionCommand(notification.Reference),
            cancellationToken);

        if (subResult.IsSuccess && subResult.Value is not null)
        {
            await RecordWebhookTransactionAsync(
                Guid.Empty,
                notification.AmountCrc,
                notification.Reference,
                "Subscription",
                subResult.Value.Id,
                cancellationToken);

            return Ok(new { activated = "subscription", id = subResult.Value.Id });
        }

        // 2. Try to activate a bounty deposit
        var bountyResult = await sender.Send(
            new ConfirmBountyDepositCommand(notification.Reference),
            cancellationToken);

        if (bountyResult.IsSuccess && bountyResult.Value is not null)
        {
            await RecordWebhookTransactionAsync(
                Guid.Empty,
                notification.AmountCrc,
                notification.Reference,
                "Bounty",
                bountyResult.Value.Id,
                cancellationToken);

            return Ok(new { activated = "bounty", id = bountyResult.Value.Id });
        }

        // 3. Try to activate a bundle order matching the reference
        var bundle = await bundleRepository.GetByPaymentReferenceAsync(notification.Reference, cancellationToken);
        if (bundle is not null && bundle.Status == BundleOrderStatus.PendingPayment)
        {
            var confirmResult = await sender.Send(new ConfirmBundlePaymentCommand(bundle.Id), cancellationToken);
            if (confirmResult.IsSuccess)
            {
                await RecordWebhookTransactionAsync(
                    bundle.UserId,
                    notification.AmountCrc,
                    notification.Reference,
                    "BundleOrder",
                    bundle.Id,
                    cancellationToken);

                return Ok(new { activated = "bundle_order", id = bundle.Id });
            }
        }

        // Reference not found — acknowledge to avoid retries but log discrepancy
        logger.LogWarning("Webhook SINPE reference {Reference} did not match any pending subscription, bounty, or bundle order.", notification.Reference);
        return Ok(new { message = "Reference not found; acknowledged." });
    }

    private async Task RecordWebhookTransactionAsync(
        Guid userId,
        decimal amountCrc,
        string reference,
        string purpose,
        Guid targetEntityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var tx = PaymentTransaction.Record(
                userId,
                amountCrc,
                $"SINPE-{reference}",
                purpose,
                targetEntityId: targetEntityId);

            tx.MarkSucceeded($"SINPE-WEBHOOK-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
            await transactionRepository.AddAsync(tx, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record audit PaymentTransaction for reference {Reference}", reference);
        }
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("public-api")]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateWebhookRequest request,
        CancellationToken cancellationToken)
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawUserId, out var userId)) return Unauthorized();
        var result = await sender.Send(new CreateWebhookSubscriptionCommand(
            userId, request.EndpointUrl, request.Secret, request.EventTypes), cancellationToken);
        return result.IsSuccess
            ? Created($"/api/webhooks/{result.Value}", new { id = result.Value })
            : UnprocessableEntity(result.Errors);
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

public sealed record CreateWebhookRequest(string EndpointUrl, string Secret, IReadOnlyList<string> EventTypes);
