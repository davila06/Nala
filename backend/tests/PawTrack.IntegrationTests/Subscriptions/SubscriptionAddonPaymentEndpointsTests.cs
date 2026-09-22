using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Subscriptions;

[Collection("Integration")]
public sealed class SubscriptionAddonPaymentEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task Replace_charges_prorated_amount_and_records_a_successful_transaction()
    {
        factory.PaymentGateway.Clear();
        var client = await AuthHelper.CreateAdminClientAsync(factory);
        var ownerId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(ownerId, SubscriptionTier.UserPlus, $"REF{Guid.NewGuid():N}"[..8], 2_990m);
        subscription.Activate();
        var addon = SubscriptionAddon.Create(
            subscription.Id, "MaxPets", 1m,
            DateTimeOffset.UtcNow.AddDays(-15), DateTimeOffset.UtcNow.AddDays(15), 3_000m);
        var profile = UserPaymentProfile.CreateCard(ownerId, "token-test", "Visa", "4242", 12, 2030, isDefault: true);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            await db.Subscriptions.AddAsync(subscription);
            await db.SubscriptionAddons.AddAsync(addon);
            await db.UserPaymentProfiles.AddAsync(profile);
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync($"/api/admin/subscription-addons/{addon.Id}/replace", new
        {
            entitlementKey = "MaxPets",
            units = 2m,
            priceCrc = 4_000m,
            expiresAt = DateTimeOffset.UtcNow.AddDays(30),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.PaymentGateway.Charges.Should().ContainSingle();
        factory.PaymentGateway.Charges[0].AmountCrc.Should().BeApproximately(2_500m, 0.02m);

        using var assertionScope = factory.Services.CreateScope();
        var assertionDb = assertionScope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var transaction = await assertionDb.PaymentTransactions.SingleAsync(item => item.TargetEntityId == subscription.Id);
        transaction.Status.Should().Be(PaymentTransactionStatus.Succeeded);
        transaction.GrossAmountCrc.Should().Be(4_000m);
        transaction.ProrationCreditCrc.Should().BeApproximately(1_500m, 0.02m);
        transaction.AmountCrc.Should().BeApproximately(2_500m, 0.02m);
    }
}
