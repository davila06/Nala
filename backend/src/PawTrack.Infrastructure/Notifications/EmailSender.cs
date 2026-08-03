using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Email sender that uses SendGrid when <c>SendGrid:ApiKey</c> is configured,
/// and falls back to structured logging (safe no-op) when it is not.
/// This allows local development without any credentials while ensuring real
/// delivery in staging/production once the Key Vault reference is set.
/// </summary>
public sealed class EmailSender(
    IConfiguration configuration,
    ILogger<EmailSender> logger)
    : IEmailSender
{
    // Resolved once per instance — Key Vault references are injected at startup.
    private string? ApiKey => configuration["SendGrid:ApiKey"];
    private string FromEmail => configuration["SendGrid:FromEmail"] ?? "noreply@pawtrack.cr";
    private string FromName => configuration["SendGrid:FromName"] ?? "PawTrack CR";
    private string BaseUrl => configuration["App:BaseUrl"] ?? "https://pawtrack.cr";

    // ── Public interface ──────────────────────────────────────────────────────

    public Task SendEmailVerificationAsync(
        string to, string name, string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/verify-email?token={Uri.EscapeDataString(verificationToken)}";
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>Confirma tu correo haciendo clic en el siguiente enlace (válido 24 h):</p>
            <p><a href="{url}">Verificar correo</a></p>
            <p>Si no creaste esta cuenta, ignora este mensaje.</p>
            """;

        return SendAsync(to, name,
            subject: "Verifica tu correo — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string to, string name, string resetToken,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>Recibimos una solicitud para restablecer tu contraseña (válida 1 h):</p>
            <p><a href="{url}">Restablecer contraseña</a></p>
            <p>Si no solicitaste esto, ignora este mensaje.</p>
            """;

        return SendAsync(to, name,
            subject: "Restablece tu contraseña — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendLostPetAlertAsync(
        string to, string ownerName, string petName,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(ownerName)},</p>
            <p>Tu mascota <strong>{Escape(petName)}</strong> ha sido marcada como perdida en PawTrack CR.</p>
            <p>Activa las notificaciones para recibir avistamientos en tiempo real.</p>
            """;

        return SendAsync(to, ownerName,
            subject: $"🐾 {petName} está perdido/a — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendPetReunitedAsync(
        string to, string ownerName, string petName,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(ownerName)},</p>
            <p>¡Qué alegría! <strong>{Escape(petName)}</strong> ha sido marcado/a como reunido/a con su familia. 🎉</p>
            <p>Gracias por usar PawTrack CR.</p>
            """;

        return SendAsync(to, ownerName,
            subject: $"🎉 ¡{petName} fue encontrado/a! — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendFamilyInvitationAsync(
        string to, string token,
        CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/familia/invitacion/{Uri.EscapeDataString(token)}";
        var html = $"""
            <p>Te han invitado a unirte a una cuenta familiar en PawTrack CR.</p>
            <p>Haz clic en el siguiente enlace para aceptar la invitación (válida 7 días):</p>
            <p><a href="{url}">Aceptar invitación</a></p>
            <p>Si no reconoces esta invitación, ignora este mensaje.</p>
            """;
        return SendAsync(to, to, subject: "Invitación a cuenta familiar — PawTrack CR", html, cancellationToken);
    }

    public Task SendSightingAlertAsync(
        string to, string ownerName, string petName,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(ownerName)},</p>
            <p>Alguien reportó un avistamiento de <strong>{Escape(petName)}</strong>.</p>
            <p>Ingresa a PawTrack CR para ver la ubicación y los detalles.</p>
            """;

        return SendAsync(to, ownerName,
            subject: $"📍 Nuevo avistamiento de {petName} — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendBroadcastLostPetAsync(
        string to, string ownerContactName, string petName,
        string petProfileUrl, string trackingUrl,
        string? recentPhotoUrl,
        DateTimeOffset lastSeenAt,
        CancellationToken cancellationToken = default)
    {
        var photo = recentPhotoUrl is not null
            ? $"""<p><img src="{recentPhotoUrl}" alt="{Escape(petName)}" style="max-width:300px;border-radius:8px" /></p>"""
            : string.Empty;

        var html = $"""
            <p>Mascota perdida cerca de tu zona de vigilancia:</p>
            <h2>{Escape(petName)}</h2>
            {photo}
            <p>Último avistamiento: {lastSeenAt:dd/MM/yyyy HH:mm} (UTC)</p>
            <p><a href="{petProfileUrl}">Ver perfil</a> · <a href="{trackingUrl}">Ver en mapa</a></p>
            <p>— Equipo PawTrack CR</p>
            """;

        return SendAsync(to, ownerContactName,
            subject: $"🚨 Mascota perdida cerca de ti: {petName} — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendFoundPetMatchAsync(
        string to, string ownerName, string petName, int scorePercent,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(ownerName)},</p>
            <p>Encontramos una posible coincidencia visual para <strong>{Escape(petName)}</strong>
               con un puntaje de similitud del <strong>{scorePercent}%</strong>.</p>
            <p>Ingresa a PawTrack CR para revisar los candidatos.</p>
            """;

        return SendAsync(to, ownerName,
            subject: $"🔍 Posible coincidencia para {petName} ({scorePercent}%) — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendStaleReportReminderAsync(
        string to, string ownerName, string petName,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(ownerName)},</p>
            <p>El reporte de pérdida de <strong>{Escape(petName)}</strong> lleva varios días activo.</p>
            <p>Si ya lo/la encontraste, márcalo/a como reunido/a en PawTrack CR para cerrar el caso.</p>
            """;

        return SendAsync(to, ownerName,
            subject: $"⏰ Recordatorio: {petName} sigue marcado/a como perdido/a — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendCustodyStartedAsync(
        string to, string recipientName, string petName,
        string counterpartName, int expectedDays,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(recipientName)},</p>
            <p>Se registró un período de custodia temporal para <strong>{Escape(petName)}</strong>
               en coordinación con <strong>{Escape(counterpartName)}</strong> ({expectedDays} días estimados).</p>
            <p>Accede a PawTrack CR para ver los detalles y actualizar el estado.</p>
            """;

        return SendAsync(to, recipientName,
            subject: $"🏠 Custodia temporal iniciada: {petName} — PawTrack CR",
            html,
            cancellationToken);
    }

    public Task SendCustodyClosedAsync(
        string to, string recipientName, string petName,
        string counterpartName, string outcome,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(recipientName)},</p>
            <p>La custodia temporal de <strong>{Escape(petName)}</strong> con
               <strong>{Escape(counterpartName)}</strong> ha finalizado.</p>
            <p>Resultado: <strong>{Escape(outcome)}</strong>.</p>
            <p>Gracias por tu ayuda. — Equipo PawTrack CR</p>
            """;

        return SendAsync(to, recipientName,
            subject: $"✅ Custodia cerrada: {petName} — PawTrack CR",
            html,
            cancellationToken);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Sends an email via SendGrid if <c>SendGrid:ApiKey</c> is set;
    /// otherwise logs a structured warning (safe no-op for local dev).
    /// </summary>
    private async Task SendAsync(
        string to, string toName,
        string subject, string htmlContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            // Dev / CI fallback — no credentials configured.
            logger.LogWarning(
                "[EmailSender] SendGrid:ApiKey not configured. Skipping real delivery. " +
                "To={To} Subject={Subject}",
                to, subject);
            return;
        }

        try
        {
            var client = new SendGridClient(ApiKey);
            var from = new EmailAddress(FromEmail, FromName);
            var toAddr = new EmailAddress(to, toName);
            var message = MailHelper.CreateSingleEmail(from, toAddr, subject, plainTextContent: null, htmlContent);
            message.SetClickTracking(enable: false, enableText: false); // privacy: no tracking pixels

            var response = await client.SendEmailAsync(message, cancellationToken);

            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                logger.LogError(
                    "[EmailSender] SendGrid returned {StatusCode} for {To}. Body={Body}",
                    (int)response.StatusCode, to, body);
            }
            else
            {
                logger.LogInformation(
                    "[EmailSender] Email sent via SendGrid. To={To} Subject={Subject} Status={Status}",
                    to, subject, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never let an email failure crash the main request — log and continue.
            logger.LogError(ex,
                "[EmailSender] Unexpected error sending email to {To} with subject {Subject}",
                to, subject);
        }
    }

    /// <summary>HTML-escapes a user-supplied string to prevent content injection.</summary>
    private static string Escape(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
