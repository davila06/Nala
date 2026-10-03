using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Stores.Events;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Stores;

[Collection("Integration")]
public sealed class StoreOrdersIdempotencyEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task PlaceOrder_ReplaysSamePayloadAndRejectsSameKeyWithDifferentPayload()
    {
        var storeOwnerEmail = $"store-idem-{Guid.NewGuid():N}@pawtrack.cr";
        var customerEmail = $"customer-idem-{Guid.NewGuid():N}@pawtrack.cr";
        using var storeOwnerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, storeOwnerEmail);
        using var customerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, customerEmail);

        Guid storeId;
        Guid productId;
        Guid customerId;
        string storeOwnerToken;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var storeOwner = await db.Users.SingleAsync(user => user.Email == storeOwnerEmail);
            storeOwner.AssignStoreRole();
            await db.SaveChangesAsync();
            storeOwnerToken = scope.ServiceProvider.GetRequiredService<IJwtTokenService>()
                .GenerateAccessToken(storeOwner.Id, storeOwner.Email, storeOwner.Name, storeOwner.Role);
            var store = Store.Create(
                storeOwner.Id, "Idempotency Test Store", "Integration test", "San Jose",
                9.9m, -84m, $"{Guid.NewGuid():N}@pawtrack.cr");
            store.Activate();
            var product = StoreProduct.Create(
                store.Id, "Integration food", null, ProductCategory.Food, 2500m, stockOnHand: 10);
            var subscription = Subscription.CreateForUser(
                storeOwner.Id, SubscriptionTier.StorePlus, $"S{Guid.NewGuid():N}"[..8], 12_000m);
            subscription.Activate();

            db.Stores.Add(store);
            db.StoreProducts.Add(product);
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync();

            storeId = store.Id;
            productId = product.Id;
            customerId = (await db.Users.SingleAsync(user => user.Email == customerEmail)).Id;
        }
        storeOwnerClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", storeOwnerToken);

        var idempotencyKey = $"order-{Guid.NewGuid():N}";
        customerClient.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        var request = new
        {
            storeId,
            fulfillmentType = "Pickup",
            deliveryAddress = (string?)null,
            customerNote = "Call on arrival",
            lines = new[] { new { productId, quantity = 2 } },
        };

        var firstResponse = await customerClient.PostAsJsonAsync("/api/store-orders", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstOrder = await firstResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var replayResponse = await customerClient.PostAsJsonAsync("/api/store-orders", request);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var replayedOrder = await replayResponse.Content.ReadFromJsonAsync<OrderResponse>();
        replayedOrder!.Id.Should().Be(firstOrder!.Id);

        var conflictingResponse = await customerClient.PostAsJsonAsync("/api/store-orders", new
        {
            storeId,
            fulfillmentType = "Pickup",
            deliveryAddress = (string?)null,
            customerNote = "Call on arrival",
            lines = new[] { new { productId, quantity = 3 } },
        });
        conflictingResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var orderId = firstOrder!.Id;
        var acceptedResponse = await storeOwnerClient.PutAsJsonAsync(
            $"/api/store-orders/{orderId}/confirm", new { note = "Disponibilidad confirmada" });
        acceptedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await customerClient.PostAsync($"/api/store-orders/{orderId}/report-payment", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        var verifiedResponse = await storeOwnerClient.PostAsJsonAsync(
            $"/api/store-orders/{orderId}/verify-payment", new { bankReference = "E2E-MANUAL-VERIFY" });
        verifiedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preparingResponse = await storeOwnerClient.PutAsJsonAsync(
            $"/api/store-orders/{orderId}/status", new { status = "Preparing", note = "Preparando" });
        preparingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var wrongFulfillmentResponse = await storeOwnerClient.PutAsJsonAsync(
            $"/api/store-orders/{orderId}/status", new { status = "OutForDelivery", note = "Invalid for pickup" });
        wrongFulfillmentResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var readyResponse = await storeOwnerClient.PutAsJsonAsync(
            $"/api/store-orders/{orderId}/status", new { status = "ReadyForPickup", note = "Listo" });
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveredResponse = await storeOwnerClient.PutAsJsonAsync(
            $"/api/store-orders/{orderId}/status", new { status = "Delivered", note = (string?)null });
        deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        (await verificationDb.StoreOrders.CountAsync(order =>
            order.CustomerId == customerId && order.IdempotencyKey == idempotencyKey)).Should().Be(1);
        var lifecycleMessages = await verificationDb.OutboxMessages.Where(message =>
            message.MessageType == typeof(StoreOrderLifecycleDomainEvent).AssemblyQualifiedName).ToListAsync();
        var lifecycleActions = lifecycleMessages
            .Select(message => JsonSerializer.Deserialize<StoreOrderLifecycleDomainEvent>(message.Payload)!.Action)
            .OrderBy(action => action)
            .ToList();
        lifecycleActions.Should().HaveCount(7).And.Contain(StoreOrderLifecycleAction.OrderPlaced)
            .And.Contain(StoreOrderLifecycleAction.OrderAccepted)
            .And.Contain(StoreOrderLifecycleAction.PaymentReported)
            .And.Contain(StoreOrderLifecycleAction.PaymentVerified);
    }

    private sealed record OrderResponse(Guid Id);
}
