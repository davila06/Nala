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
        IReadOnlyList<NearbyClinicRef>? nearbyFeaturedClinics = null,
        CancellationToken cancellationToken = default)
    {
        var photo = recentPhotoUrl is not null
            ? $"""<p><img src="{recentPhotoUrl}" alt="{Escape(petName)}" style="max-width:300px;border-radius:8px" /></p>"""
            : string.Empty;

        // Real, visible clinic logo — HTML email is the one channel that renders inline
        // images reliably without a separate media message.
        var clinicsHtml = nearbyFeaturedClinics is { Count: > 0 }
            ? $"""
                <div style="margin-top:16px;padding-top:12px;border-top:1px solid #eee;">
                  <p style="font-size:13px;color:#666;margin:0 0 8px;">🏥 Clínicas veterinarias cercanas:</p>
                  {string.Join("", nearbyFeaturedClinics.Select(c => $"""
                    <div style="display:flex;align-items:center;gap:8px;margin-bottom:6px;">
                      {(c.LogoUrl is not null
                          ? $"""<img src="{c.LogoUrl}" alt="{Escape(c.Name)}" width="32" height="32" style="border-radius:6px;object-fit:cover;" />"""
                          : "")}
                      <span style="font-size:13px;color:#333;">{Escape(c.Name)}{(c.PhoneNumber is not null ? $" — {Escape(c.PhoneNumber)}" : "")}</span>
                    </div>
                    """))}
                </div>
                """
            : string.Empty;

        var html = $"""
            <p>Mascota perdida cerca de tu zona de vigilancia:</p>
            <h2>{Escape(petName)}</h2>
            {photo}
            <p>Último avistamiento: {lastSeenAt:dd/MM/yyyy HH:mm} (UTC)</p>
            <p><a href="{petProfileUrl}">Ver perfil</a> · <a href="{trackingUrl}">Ver en mapa</a></p>
            {clinicsHtml}
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

    // ── Bundle order emails ───────────────────────────────────────────────────

    public Task SendBundleOrderConfirmationAsync(
        string to, string name, string collarModelLabel,
        string paymentReference, decimal amountCrc, string shippingAddress,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>¡Tu pedido de <strong>{Escape(collarModelLabel)}</strong> + 12 meses de PawTrack Plus
               fue recibido correctamente!</p>
            <h3>Detalles del pedido</h3>
            <table style="border-collapse:collapse;width:100%;max-width:480px;">
              <tr><td style="padding:6px 0;color:#6b6057;">Collar</td><td style="font-weight:700;">{Escape(collarModelLabel)}</td></tr>
              <tr><td style="padding:6px 0;color:#6b6057;">Plan incluido</td><td style="font-weight:700;">PawTrack Plus · 12 meses</td></tr>
              <tr><td style="padding:6px 0;color:#6b6057;">Total</td><td style="font-weight:700;">₡{amountCrc:N0}</td></tr>
              <tr><td style="padding:6px 0;color:#6b6057;">Referencia SINPE</td><td style="font-weight:800;font-size:1.3em;color:#1a3484;">{Escape(paymentReference)}</td></tr>
              <tr><td style="padding:6px 0;color:#6b6057;">Dirección de envío</td><td>{Escape(shippingAddress)}</td></tr>
            </table>
            <h3>¿Qué sigue?</h3>
            <ol>
              <li>Realiza la transferencia SINPE Móvil al número configurado con la referencia <strong>{Escape(paymentReference)}</strong>.</li>
              <li>Marca el pago como realizado en tu perfil de PawTrack.</li>
              <li>Confirmaremos el pago en 24-48 horas hábiles y activaremos tu plan Plus.</li>
              <li>Adquiriremos tu collar y te enviaremos el número de seguimiento por correo.</li>
            </ol>
            <p>¿Preguntas? Escríbenos a <a href="mailto:soporte@pawtrack.cr">soporte@pawtrack.cr</a></p>
            """;

        return SendAsync(to, name,
            subject: $"📦 Pedido recibido: {collarModelLabel} — PawTrack CR",
            html, cancellationToken);
    }

    public Task SendBundlePaymentConfirmedAsync(
        string to, string name, string collarModelLabel,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>✅ ¡Confirmamos tu pago para el bundle <strong>{Escape(collarModelLabel)}</strong>!</p>
            <p>Tu plan <strong>PawTrack Plus por 12 meses</strong> ya está activo en tu cuenta.
               Puedes conectar tu collar GPS desde la pestaña GPS en el perfil de tu mascota.</p>
            <p>Estaremos adquiriendo tu collar y te notificaremos con el número de rastreo
               tan pronto como sea despachado.</p>
            <p>Gracias por confiar en PawTrack CR. 🐾</p>
            """;

        return SendAsync(to, name,
            subject: $"✅ Pago confirmado · Plan Plus activado — PawTrack CR",
            html, cancellationToken);
    }

    public Task SendBundleShippedAsync(
        string to, string name, string collarModelLabel, string trackingNumber,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>📦 ¡Tu <strong>{Escape(collarModelLabel)}</strong> está en camino!</p>
            <p><strong>Número de seguimiento:</strong> {Escape(trackingNumber)}</p>
            <p>Puedes rastrear tu paquete en el sitio del transportista con el código anterior.
               El tiempo estimado de entrega es de 2-5 días hábiles.</p>
            <p>Una vez que lo recibas, conecta el collar desde la pestaña GPS 📡 en el perfil de tu mascota.</p>
            <p>Cualquier duda: <a href="mailto:soporte@pawtrack.cr">soporte@pawtrack.cr</a></p>
            """;

        return SendAsync(to, name,
            subject: $"🚚 Tu collar GPS está en camino — PawTrack CR",
            html, cancellationToken);
    }

    // ── Subscription lifecycle emails ─────────────────────────────────────────

    public Task SendSubscriptionExpiringAsync(
        string to, string name, string tierLabel, DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var days = (int)Math.Ceiling((expiresAt - DateTimeOffset.UtcNow).TotalDays);
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>Tu plan <strong>{Escape(tierLabel)}</strong> en PawTrack CR vence en <strong>{days} día{(days == 1 ? "" : "s")}</strong>
               ({expiresAt:dd/MM/yyyy}).</p>
            <p>Para renovarlo y no perder tus beneficios, ingresa a tu perfil y realiza el pago
               antes de esa fecha.</p>
            <p><a href="https://pawtrack.cr/perfil">Renovar plan →</a></p>
            <p>Gracias por ser parte de PawTrack CR. 🐾</p>
            """;
        return SendAsync(to, name,
            subject: $"⏰ Tu plan {tierLabel} vence en {days} día{(days == 1 ? "" : "s")} — PawTrack CR",
            html, cancellationToken);
    }

    public Task SendSubscriptionExpiredAsync(
        string to, string name, string tierLabel,
        CancellationToken cancellationToken = default)
    {
        var html = $"""
            <p>Hola {Escape(name)},</p>
            <p>Tu plan <strong>{Escape(tierLabel)}</strong> en PawTrack CR ha vencido.</p>
            <p>Tus funciones premium están temporalmente desactivadas hasta que renueves tu suscripción.</p>
            <p><a href="https://pawtrack.cr/perfil">Renovar plan →</a></p>
            <p>¿Tienes dudas? Escríbenos a <a href="mailto:soporte@pawtrack.cr">soporte@pawtrack.cr</a></p>
            """;
        return SendAsync(to, name,
            subject: $"Tu plan {tierLabel} ha vencido — PawTrack CR",
            html, cancellationToken);
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

            // Retry up to 3 times on transient errors (5xx, network failures).
            Response? response = null;
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                response = await client.SendEmailAsync(message, cancellationToken);
                if ((int)response.StatusCode < 500) break;
                if (attempt < 3)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
            }

            if ((int)response!.StatusCode >= 400)
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
