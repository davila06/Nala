using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Notifications;

namespace PawTrack.IntegrationTests.Infrastructure;

public sealed class TestLogger : Microsoft.Extensions.Logging.ILogger<EmailSender>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}

public sealed class SendAllScenariosEmailRunner
{
    private const string TargetEmail = "davila@itqscr.com";
    private const string TargetName = "Denis Ávila";

    [Fact]
    public async Task SendAll33TransactionalEmailScenarios()
    {
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");

        var inMemoryConfig = new Dictionary<string, string?>
        {
            ["SendGrid:FromEmail"] = "davila06@gmail.com",
            ["SendGrid:FromName"] = "PawTrack CR",
            ["App:BaseUrl"] = "https://pawtrack.cr"
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            inMemoryConfig["SendGrid:ApiKey"] = apiKey;
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemoryConfig)
            .Build();

        var logger = new TestLogger();
        var sender = new EmailSender(config, logger);
        var tasks = new List<Task>
        {
            // 1. Auth & Security
            sender.SendEmailVerificationAsync(TargetEmail, TargetName, "VERIFY-123456-TOKEN"),
            sender.SendPasswordResetAsync(TargetEmail, TargetName, "RESET-987654-TOKEN"),
            sender.SendPasswordResetSuccessAsync(TargetEmail, TargetName),
            sender.SendFamilyInvitationAsync(TargetEmail, "FAM-INVITE-TOKEN"),

            // 2. Lost Pet & Recovery
            sender.SendLostPetAlertAsync(TargetEmail, TargetName, "Nala"),
            sender.SendPetReunitedAsync(TargetEmail, TargetName, "Nala"),
            sender.SendSightingAlertAsync(TargetEmail, TargetName, "Nala"),
            sender.SendBroadcastLostPetAsync(
                TargetEmail, TargetName, "Nala",
                "https://pawtrack.cr/pet/nala", "https://pawtrack.cr/map/nala",
                "https://pawtrack.cr/static/photos/nala.jpg",
                DateTimeOffset.UtcNow,
                [new NearbyClinicRef("Clínica Veterinaria Escazú", "2288-0000", "Escazú, San José", "https://pawtrack.cr/logo-clinic.png")]),
            sender.SendFoundPetMatchAsync(TargetEmail, TargetName, "Nala", 92),
            sender.SendStaleReportReminderAsync(TargetEmail, TargetName, "Nala"),

            // 3. Custody / Fosters
            sender.SendCustodyStartedAsync(TargetEmail, TargetName, "Nala", "Casa Cuna San José", 7),
            sender.SendCustodyClosedAsync(TargetEmail, TargetName, "Nala", "Casa Cuna San José", "Mascota reunida exitosamente"),

            // 4. Bundles & Products
            sender.SendBundleOrderConfirmationAsync(TargetEmail, TargetName, "Collar GPS PawTrack Pro", "REF-SINPE-8849", 45000m, "Escazú, San José"),
            sender.SendBundlePaymentConfirmedAsync(TargetEmail, TargetName, "Collar GPS PawTrack Pro"),
            sender.SendBundleShippedAsync(TargetEmail, TargetName, "Collar GPS PawTrack Pro", "CR-POSTAL-992831"),

            // 5. Subscriptions & Billing
            sender.SendSubscriptionExpiringAsync(TargetEmail, TargetName, "PawTrack Plus", DateTimeOffset.UtcNow.AddDays(7)),
            sender.SendSubscriptionExpiredAsync(TargetEmail, TargetName, "PawTrack Plus"),
            sender.SendRecurringPaymentReceiptAsync(TargetEmail, TargetName, "PawTrack Plus (Anual)", 28000m, "4242", DateTimeOffset.UtcNow.AddYears(1)),
            sender.SendRecurringPaymentFailedAsync(TargetEmail, TargetName, "PawTrack Plus", "Fondos insuficientes", DateTimeOffset.UtcNow.AddDays(3)),

            // 6. Partners & Verification
            sender.SendClinicApprovedWelcomeAsync(TargetEmail, "Veterinaria PetCare", "https://pawtrack.cr/login"),
            sender.SendStoreApprovedWelcomeAsync(TargetEmail, "SuperPet Store", "https://pawtrack.cr/login"),
            sender.SendStoreReviewedNoticeAsync(TargetEmail, "SuperPet Store", true),
            sender.SendServiceProviderApprovedWelcomeAsync(TargetEmail, "Grooming Express", "https://pawtrack.cr/login"),
            sender.SendServiceProviderReviewedNoticeAsync(TargetEmail, "Grooming Express", true),

            // 7. Adoptions
            sender.SendAdoptionInterestAsync(TargetEmail, "Refugio Mascotas de Costa Rica", "Rocky", TargetName, "APP-9988"),
            sender.SendAdoptionApprovedAsync(TargetEmail, TargetName, "Rocky"),
            sender.SendAdoptionRejectedAsync(TargetEmail, TargetName, "Rocky"),

            // 8. Service Bookings
            sender.SendProviderBookingCreatedCustomerAsync(TargetEmail, TargetName, "Grooming Express", "Baño y Corte Canino", DateTimeOffset.UtcNow.AddDays(2)),
            sender.SendProviderBookingCreatedProviderAsync(TargetEmail, "Grooming Express", TargetName, "Baño y Corte Canino", DateTimeOffset.UtcNow.AddDays(2)),

            // 9. Store Orders
            sender.SendStoreOrderPlacedCustomerAsync(TargetEmail, TargetName, "PetShop Curridabat", "ORD-7711", 12500m),
            sender.SendStoreOrderPlacedStoreAsync(TargetEmail, "PetShop Curridabat", TargetName, "ORD-7711", 12500m),
            sender.SendStoreOrderConfirmedCustomerAsync(TargetEmail, TargetName, "PetShop Curridabat", "ORD-7711", "Tu pedido está empacado y listo para retirar"),

            // 10. Safety & Collars
            sender.SendCollarSafeZoneBreachAsync(TargetEmail, TargetName, "Nala", "Jardín Casa Escazú")
        };

        await Task.WhenAll(tasks);
    }
}
