using PawTrack.Application.Common.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

/// <summary>No-op email sender that captures the last verification token for use in tests.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastVerificationToken { get; private set; }

    public Task SendEmailVerificationAsync(string to, string name, string verificationToken, CancellationToken ct = default)
    {
        LastVerificationToken = verificationToken;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string to, string name, string resetToken, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendLostPetAlertAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendPetReunitedAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendSightingAlertAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBroadcastLostPetAsync(string to, string ownerContactName, string petName, string petProfileUrl,
        string trackingUrl, string? recentPhotoUrl, DateTimeOffset lastSeenAt, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendFoundPetMatchAsync(string to, string ownerName, string petName, int scorePercent, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendStaleReportReminderAsync(string to, string ownerName, string petName, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendCustodyStartedAsync(string to, string recipientName, string petName, string counterpartName,
        int expectedDays, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendCustodyClosedAsync(string to, string recipientName, string petName, string counterpartName,
        string outcome, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendFamilyInvitationAsync(string to, string token, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBundleOrderConfirmationAsync(string to, string recipientName, string productName,
        string orderId, decimal price, string currency, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendBundlePaymentConfirmedAsync(string to, string recipientName, string orderId, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SendBundleShippedAsync(string to, string recipientName, string orderId,
        string trackingCode, CancellationToken ct = default) => Task.CompletedTask;
}
