using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PawTrack.Domain.Subscriptions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Subscriptions;

[Collection("Integration")]
public sealed class PublicSubscriptionPlansEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task GetActive_Unauthenticated_ReturnsCatalog()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/catalog/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CommercialApproval_AuthenticatedOwner_ReturnsForbidden()
    {
        using var owner = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await owner.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{Guid.NewGuid()}/commercial-approval",
            new { Version = Guid.NewGuid(), ApprovalReference = "APPROVAL-REF-001" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetActive_ReturnsPersistedPrice_AndOmitsDeactivatedPlan()
    {
        using var admin = await AuthHelper.CreateAdminClientAsync(factory);
        var displayName = $"Plus catalog test {Guid.NewGuid():N}";
        var createResponse = await admin.PostAsJsonAsync(
            "/api/admin/subscription-plans",
            new
            {
                Tier = SubscriptionTier.UserPlus,
                DisplayName = displayName,
                Description = "Integration test plan",
                MonthlyPriceCrc = 3000m,
                AnnualPriceCrc = (decimal?)null,
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var planId = createdJson.RootElement.GetProperty("id").GetGuid();
        var version = createdJson.RootElement.GetProperty("version").GetGuid();
        using var publicClient = factory.CreateClient();

        var invalidApprovalResponse = await admin.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{planId}/commercial-approval",
            new { Version = version, ApprovalReference = "LEGAL approval includes private details" });
        invalidApprovalResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var activeResponse = await publicClient.GetAsync("/api/catalog/subscription-plans");
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var activeJson = JsonDocument.Parse(await activeResponse.Content.ReadAsStringAsync());
        activeJson.RootElement.EnumerateArray()
            .Should().NotContain(plan => plan.GetProperty("displayName").GetString() == displayName);

        var approveResponse = await admin.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{planId}/commercial-approval",
            new { Version = version, ApprovalReference = "LEGAL-APPROVAL-REF-2026-10" });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var approvedJson = JsonDocument.Parse(await approveResponse.Content.ReadAsStringAsync());
        version = approvedJson.RootElement.GetProperty("version").GetGuid();

        var publishedResponse = await publicClient.GetAsync("/api/catalog/subscription-plans");
        publishedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var publishedJson = JsonDocument.Parse(await publishedResponse.Content.ReadAsStringAsync());
        var activePlan = publishedJson.RootElement.EnumerateArray()
            .Single(plan => plan.GetProperty("displayName").GetString() == displayName);
        activePlan.GetProperty("monthlyPriceCrc").GetDecimal().Should().Be(3000m);
        activePlan.TryGetProperty("commercialApprovalReference", out _).Should().BeFalse();

        using var revokeRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/admin/subscription-plans/{planId}/commercial-approval")
        {
            Content = JsonContent.Create(new { Version = version }),
        };
        var revokeResponse = await admin.SendAsync(revokeRequest);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var revokedJson = JsonDocument.Parse(await revokeResponse.Content.ReadAsStringAsync());
        version = revokedJson.RootElement.GetProperty("version").GetGuid();

        var afterRevocationResponse = await publicClient.GetAsync("/api/catalog/subscription-plans");
        afterRevocationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterRevocationJson = JsonDocument.Parse(
            await afterRevocationResponse.Content.ReadAsStringAsync());
        afterRevocationJson.RootElement.EnumerateArray()
            .Should().NotContain(plan => plan.GetProperty("displayName").GetString() == displayName);

        var reapproveResponse = await admin.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{planId}/commercial-approval",
            new { Version = version, ApprovalReference = "LEGAL-APPROVAL-REF-2026-10" });
        reapproveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var reapprovedJson = JsonDocument.Parse(await reapproveResponse.Content.ReadAsStringAsync());
        version = reapprovedJson.RootElement.GetProperty("version").GetGuid();

        using var deactivateRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/api/admin/subscription-plans/{planId}")
        {
            Content = JsonContent.Create(new { Version = version }),
        };
        var deactivateResponse = await admin.SendAsync(deactivateRequest);
        deactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterDeactivationResponse = await publicClient.GetAsync("/api/catalog/subscription-plans");
        afterDeactivationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var afterDeactivationJson = JsonDocument.Parse(
            await afterDeactivationResponse.Content.ReadAsStringAsync());
        afterDeactivationJson.RootElement.EnumerateArray()
            .Should().NotContain(plan => plan.GetProperty("displayName").GetString() == displayName);
    }
}
