using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Email sender stub — logs emails in development.
/// Replace with SendGrid / Azure Communication Services in production.
/// </summary>
public sealed class EmailSender(
    IConfiguration configuration,
    ILogger<EmailSender> logger)
    : IEmailSender
{
    public async Task SendEmailVerificationAsync(
        string to,
        string name,
        string verificationToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5001";

        // NOTE: the full URL (including the token) is intentionally NOT logged here —
        // verification tokens are single-use credentials and must not appear in log sinks
        // (Application Insights, stdout, etc.).  The token is sent only in the email body.
        logger.LogInformation(
            "Sending email verification to {To} [stub — production: SendGrid]",
            to);

        // TODO Sprint 3: replace with SendGrid or Azure Communication Services
        // The email body should include: {baseUrl}/verify-email?token={verificationToken}
        _ = baseUrl; // suppress unused-variable warning until real sender is wired up
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetAsync(
        string to,
        string name,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = configuration["App:BaseUrl"] ?? "https://localhost:5001";

        logger.LogInformation(
            "Sending password-reset email to {To} [stub — production: SendGrid]",
            to);

        // TODO Sprint 3: replace with SendGrid or Azure Communication Services.
        // The email body should include: {baseUrl}/reset-password?token={resetToken}
        _ = baseUrl;
        _ = name;
        _ = resetToken;
        await Task.CompletedTask;
    }

    public async Task SendLostPetAlertAsync(
        string to,
        string ownerName,
        string petName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending lost pet alert email to {To} for pet {PetName}",
            to, petName);

        // TODO: replace with SendGrid template send
        await Task.CompletedTask;
    }

    public async Task SendPetReunitedAsync(
        string to,
        string ownerName,
        string petName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending pet reunited email to {To} for pet {PetName}",
            to, petName);

        // TODO: replace with SendGrid template send
        await Task.CompletedTask;
    }

    public async Task SendSightingAlertAsync(
        string to,
        string ownerName,
        string petName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending sighting alert email to {To} for pet {PetName}",
            to, petName);

        // TODO Sprint 4 Go-Live: replace with SendGrid template send
        await Task.CompletedTask;
    }

    public async Task SendBroadcastLostPetAsync(
        string to,
        string ownerContactName,
        string petName,
        string petProfileUrl,
        string trackingUrl,
        string? recentPhotoUrl,
        DateTimeOffset lastSeenAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending broadcast lost-pet email to {To} for {PetName}. Profile: {Url} Tracking: {Tracking}",
            to, petName, petProfileUrl, trackingUrl);

        // TODO Go-Live: send via SendGrid / Azure Communication Services.
        // Template variables: ownerContactName, petName, petProfileUrl,
        // trackingUrl, recentPhotoUrl (optional), lastSeenAt (formatted local time CR).
        await Task.CompletedTask;
    }

    public async Task SendFoundPetMatchAsync(
        string to,
        string ownerName,
        string petName,
        int scorePercent,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending found-pet match email to {To} for {PetName} with score {ScorePercent}%",
            to,
            petName,
            scorePercent);

        await Task.CompletedTask;
    }

    public async Task SendStaleReportReminderAsync(
        string to,
        string ownerName,
        string petName,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending stale-report reminder email to {To} for {PetName}",
            to,
            petName);

        await Task.CompletedTask;
    }

    public async Task SendCustodyStartedAsync(
        string to,
        string recipientName,
        string petName,
        string counterpartName,
        int expectedDays,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending custody-started email to {To} for pet {PetName}. Recipient={Recipient}; Counterpart={Counterpart}; ExpectedDays={ExpectedDays}",
            to,
            petName,
            recipientName,
            counterpartName,
            expectedDays);

        await Task.CompletedTask;
    }

    public async Task SendCustodyClosedAsync(
        string to,
        string recipientName,
        string petName,
        string counterpartName,
        string outcome,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Sending custody-closed email to {To} for pet {PetName}. Recipient={Recipient}; Counterpart={Counterpart}; Outcome={Outcome}",
            to,
            petName,
            recipientName,
            counterpartName,
            outcome);

        await Task.CompletedTask;
    }
}

