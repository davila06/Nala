using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;

/// <summary>
/// Processes a single inbound WhatsApp message from a Meta Cloud API webhook.
/// The controller parses the raw webhook payload and dispatches one command per message.
/// </summary>
/// <param name="WaId">Sender's WhatsApp phone number (E.164 without '+', e.g. "50612345678").</param>
/// <param name="MessageId">Unique wamid from Meta — used for idempotency.</param>
/// <param name="MessageType">Meta message type: "text", "location", "image", etc.</param>
/// <param name="TextBody">Body text when <paramref name="MessageType"/> is "text".</param>
/// <param name="LocationLat">Latitude when <paramref name="MessageType"/> is "location".</param>
/// <param name="LocationLng">Longitude when <paramref name="MessageType"/> is "location".</param>
/// <param name="LocationAddress">Optional address label sent by WhatsApp with a location pin.</param>
public sealed record HandleWhatsAppWebhookCommand(
    string WaId,
    string MessageId,
    string MessageType,
    string? TextBody,
    double? LocationLat,
    double? LocationLng,
    string? LocationAddress) : IRequest<Result<Unit>>;
