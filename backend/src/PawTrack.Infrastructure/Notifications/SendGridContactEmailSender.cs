using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Notifications;

public sealed class SendGridContactEmailSender(
    IHttpClientFactory clients,
    IConfiguration configuration,
    ILogger<SendGridContactEmailSender> logger) : IContactEmailSender
{
    private const string SendGridClientName = "ContactSendGrid";
    private const string DefaultContactInbox = "soporte@pawtrack.cr";

    public async Task<bool> SendAsync(ContactEmailMessage message, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["SendGrid:ApiKey"];
        var fromEmail = configuration["SendGrid:FromEmail"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(fromEmail))
        {
            logger.LogWarning("SendGrid contact email is not configured.");
            return false;
        }

        var fromName = configuration["SendGrid:FromName"] ?? "PawTrack CR";
        var recipient = configuration["Contact:ToEmail"] ?? DefaultContactInbox;
        var body = $"Nombre: {message.Name}\nCorreo de respuesta: {message.ReplyTo}\nTema: {message.Topic}\n\n{message.Message}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new
        {
            personalizations = new[] { new { to = new[] { new { email = recipient } } } },
            from = new { email = fromEmail, name = fromName },
            reply_to = new { email = message.ReplyTo, name = message.Name },
            subject = $"Contacto PawTrack CR · {message.Topic}",
            content = new[] { new { type = "text/plain", value = body } },
            tracking_settings = new
            {
                click_tracking = new { enable = false, enable_text = false },
                open_tracking = new { enable = false },
            },
        });

        try
        {
            using var response = await clients.CreateClient(SendGridClientName).SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Accepted)
                return true;

            logger.LogWarning("SendGrid did not accept contact email. StatusCode={StatusCode}", (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not reach SendGrid for contact email.");
            return false;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "SendGrid contact email request timed out.");
            return false;
        }
    }
}
