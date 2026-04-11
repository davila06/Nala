using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Bot.Queries.VerifyWhatsAppWebhook;

/// <summary>
/// Meta Cloud API performs a GET request to verify the webhook endpoint.
/// The handler validates the <c>hub.verify_token</c> and echoes back the challenge.
/// </summary>
/// <param name="HubMode">Must equal "subscribe".</param>
/// <param name="HubVerifyToken">Must match <c>WhatsApp:VerifyToken</c> in configuration.</param>
/// <param name="HubChallenge">Random string from Meta that must be echoed back.</param>
public sealed record VerifyWhatsAppWebhookQuery(
    string HubMode,
    string HubVerifyToken,
    string HubChallenge) : IRequest<Result<string>>;

public sealed class VerifyWhatsAppWebhookQueryHandler(IWhatsAppSettings settings)
    : IRequestHandler<VerifyWhatsAppWebhookQuery, Result<string>>
{
    public Task<Result<string>> Handle(
        VerifyWhatsAppWebhookQuery request, CancellationToken cancellationToken)
    {
        if (request.HubMode != "subscribe")
            return Task.FromResult(Result.Failure<string>("Invalid hub.mode."));

        if (string.IsNullOrWhiteSpace(settings.VerifyToken))
            return Task.FromResult(Result.Failure<string>("WhatsApp verify token not configured."));

        if (!string.Equals(request.HubVerifyToken, settings.VerifyToken, StringComparison.Ordinal))
            return Task.FromResult(Result.Failure<string>("Verify token mismatch."));

        return Task.FromResult(Result.Success(request.HubChallenge));
    }
}
