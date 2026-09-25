using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Infrastructure.Clinics;

public sealed class SendGridClinicEmailGateway(IHttpClientFactory clients, IConfiguration configuration) : IClinicEmailGateway
{
    public async Task<Result<string>> SendAsync(string recipient, string subject, string text, Guid requestId, CancellationToken cancellationToken = default)
    {
        var key = configuration["SendGrid:ApiKey"];
        var from = configuration["SendGrid:FromEmail"];
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(from))
            return Result.Failure<string>("Proveedor de correo clínico no configurado.");
        if (string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(text))
            return Result.Failure<string>("Destinatario y contenido requeridos.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = JsonContent.Create(new
        {
            personalizations = new[] { new { to = new[] { new { email = recipient } } } },
            from = new { email = from },
            subject,
            content = new[] { new { type = "text/plain", value = text } },
            custom_args = new { clinic_request_id = requestId.ToString("N") },
            tracking_settings = new { click_tracking = new { enable = false, enable_text = false }, open_tracking = new { enable = false } },
        });

        try
        {
            using var response = await clients.CreateClient("ClinicSendGrid").SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Accepted || !response.Headers.TryGetValues("X-Message-Id", out var values))
                return Result.Failure<string>("El proveedor no confirmó la aceptación del correo.");
            var providerId = values.FirstOrDefault();
            return string.IsNullOrWhiteSpace(providerId) ? Result.Failure<string>("Falta identificador del proveedor.") : Result.Success(providerId);
        }
        catch (HttpRequestException)
        {
            return Result.Failure<string>("No se pudo contactar al proveedor de correo.");
        }
    }
}
