using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Filters;
using PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;
using PawTrack.Application.Bot.Queries.VerifyWhatsAppWebhook;
using PawTrack.Application.Common.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PawTrack.API.Controllers;

/// <summary>
/// Webhook endpoint for the Meta Cloud API (WhatsApp Business).
/// <para>
/// GET  /api/whatsapp/webhook — Meta verification handshake (hub.challenge echo).
/// POST /api/whatsapp/webhook — Inbound message handler (signature-validated).
/// </para>
/// <para>
/// The POST action uses <see cref="ValidateWhatsAppSignatureAttribute"/> to verify
/// the HMAC-SHA256 signature before any payload processing takes place.
/// Body buffering (Request.EnableBuffering) is configured in Program.cs middleware
/// so that the filter can read the body and the action can bind it.
/// </para>
/// </summary>
[ApiController]
[Route("api/whatsapp")]
[AllowAnonymous]           // Webhooks come from Meta servers — no auth header
[ValidateWhatsAppSignature] // HMAC guard on POST; passthrough on GET
public sealed class WhatsAppController(
    ISender sender,
    IBroadcastAttemptRepository? attemptRepository,
    IUnitOfWork? unitOfWork,
    ILogger<WhatsAppController> logger)
    : ControllerBase
{
    public WhatsAppController(ISender sender, ILogger<WhatsAppController> logger)
        : this(sender, null, null, logger) { }
    // ── GET /api/whatsapp/webhook — Meta verification handshake ───────────────

    [HttpGet("webhook")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Verify(
        [FromQuery(Name = "hub.mode")] string? hubMode,
        [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken,
        [FromQuery(Name = "hub.challenge")] string? hubChallenge,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(hubMode)
            || string.IsNullOrWhiteSpace(hubVerifyToken)
            || string.IsNullOrWhiteSpace(hubChallenge))
            return Forbid();

        var result = await sender.Send(
            new VerifyWhatsAppWebhookQuery(hubMode, hubVerifyToken, hubChallenge), ct);

        return result.IsSuccess ? Ok(result.Value) : Forbid();
    }

    // ── POST /api/whatsapp/webhook — Inbound messages & delivery statuses ────

    [HttpPost("webhook")]
    [EnableRateLimiting("sightings")] // broad shared bucket — webhook traffic is low
    [RequestSizeLimit(102_400)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveWebhook(
        [FromBody] MetaWebhookPayload payload,
        CancellationToken ct)
    {
        // Meta expects a 200 OK within 5 s regardless of processing outcome.
        try
        {
            foreach (var entry in payload.Entry ?? [])
                foreach (var change in entry.Changes ?? [])
                {
                    var messages = change.Value?.Messages ?? [];
                    foreach (var msg in messages)
                    {
                        if (string.IsNullOrWhiteSpace(msg.From) || string.IsNullOrWhiteSpace(msg.Id))
                            continue;

                        var command = new HandleWhatsAppWebhookCommand(
                            WaId: msg.From,
                            MessageId: msg.Id,
                            MessageType: msg.Type ?? "text",
                            TextBody: msg.Text?.Body,
                            LocationLat: msg.Location?.Latitude,
                            LocationLng: msg.Location?.Longitude,
                            LocationAddress: msg.Location?.Address);

                        await sender.Send(command, ct);
                    }

                    // Process delivery status receipts (sent, delivered, read, failed)
                    if (attemptRepository is not null && unitOfWork is not null)
                    {
                        var statuses = change.Value?.Statuses ?? [];
                        foreach (var st in statuses)
                        {
                            if (string.IsNullOrWhiteSpace(st.Id)) continue;
                            var attempt = await attemptRepository.GetByExternalIdAsync(st.Id, ct);
                            if (attempt is not null)
                            {
                                if (string.Equals(st.Status, "failed", StringComparison.OrdinalIgnoreCase))
                                {
                                    var errorMsg = st.Errors?.FirstOrDefault()?.Title ?? "WhatsApp delivery failed";
                                    attempt.MarkFailed(errorMsg);
                                    attemptRepository.Update(attempt);
                                }
                                else if (string.Equals(st.Status, "delivered", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(st.Status, "sent", StringComparison.OrdinalIgnoreCase))
                                {
                                    attempt.MarkSent(st.Id);
                                    attemptRepository.Update(attempt);
                                }
                                await unitOfWork.SaveChangesAsync(ct);
                            }
                        }
                    }
                }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error processing WhatsApp webhook payload.");
        }

        return Ok();
    }

    // ── Meta Cloud API payload DTOs ───────────────────────────────────────────
    // Source-generated JSON binding; no reflection at runtime.

    public sealed class MetaWebhookPayload
    {
        [JsonPropertyName("object")] public string? Object { get; set; }
        [JsonPropertyName("entry")] public List<MetaEntry>? Entry { get; set; }
    }

    public sealed class MetaEntry
    {
        [JsonPropertyName("changes")] public List<MetaChange>? Changes { get; set; }
    }

    public sealed class MetaChange
    {
        [JsonPropertyName("value")] public MetaChangeValue? Value { get; set; }
        [JsonPropertyName("field")] public string? Field { get; set; }
    }

    public sealed class MetaChangeValue
    {
        [JsonPropertyName("messages")] public List<MetaMessage>? Messages { get; set; }
        [JsonPropertyName("statuses")] public List<MetaStatus>? Statuses { get; set; }
        [JsonPropertyName("contacts")] public List<MetaContact>? Contacts { get; set; }
    }

    public sealed class MetaStatus
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("recipient_id")] public string? RecipientId { get; set; }
        [JsonPropertyName("errors")] public List<MetaStatusError>? Errors { get; set; }
    }

    public sealed class MetaStatusError
    {
        [JsonPropertyName("code")] public int? Code { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    public sealed class MetaMessage
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("from")] public string? From { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("text")] public MetaTextBody? Text { get; set; }
        [JsonPropertyName("location")] public MetaLocation? Location { get; set; }
    }

    public sealed class MetaTextBody
    {
        [JsonPropertyName("body")] public string? Body { get; set; }
    }

    public sealed class MetaLocation
    {
        [JsonPropertyName("latitude")] public double? Latitude { get; set; }
        [JsonPropertyName("longitude")] public double? Longitude { get; set; }
        [JsonPropertyName("address")] public string? Address { get; set; }
    }

    public sealed class MetaContact
    {
        [JsonPropertyName("wa_id")] public string? WaId { get; set; }
        [JsonPropertyName("profile")] public MetaProfile? Profile { get; set; }
    }

    public sealed class MetaProfile
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
