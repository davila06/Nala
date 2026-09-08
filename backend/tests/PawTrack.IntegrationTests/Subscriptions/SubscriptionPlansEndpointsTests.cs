using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.Domain.Subscriptions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Subscriptions;

// Minimal projection avoiding SubscriptionTier enum deserialization (default JsonSerializerOptions
// has no JsonStringEnumConverter registered client-side, unlike the API's own response serializer).
file sealed record PlanDto(Guid Id, string DisplayName, decimal? MonthlyPriceCrc, decimal? AnnualPriceCrc, string? Description, Guid Version);

[Collection("Integration")]
public sealed class SubscriptionPlansEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task GetPlans_Unauthenticated_Returns401()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlans_AuthenticatedOwner_Returns403()
    {
        using var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/admin/subscription-plans");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Regression test: Update handler previously never called SaveChangesAsync, so the PUT
    // response looked correct (in-memory entity) but the change was never committed to the DB.
    [Fact]
    public async Task UpdatePlan_PersistsChange_AfterRefetch()
    {
        var admin = await AuthHelper.CreateAdminClientAsync(factory);

        var createResp = await admin.PostAsJsonAsync(
            "/api/admin/subscription-plans",
            new
            {
                Tier = SubscriptionTier.ShelterPlus,
                DisplayName = "Original name",
                Description = "Original description",
                MonthlyPriceCrc = (decimal?)8000,
                AnnualPriceCrc = (decimal?)null,
            });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<PlanDto>();
        var newDisplayName = $"Updated {Guid.NewGuid():N}";

        var updateResp = await admin.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{created!.Id}",
            new
            {
                Version = created.Version,
                DisplayName = newDisplayName,
                created.Description,
                created.MonthlyPriceCrc,
                created.AnnualPriceCrc,
            });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var refetchResp = await admin.GetAsync($"/api/admin/subscription-plans/{created.Id}");
        refetchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refetched = await refetchResp.Content.ReadFromJsonAsync<PlanDto>();
        refetched!.DisplayName.Should().Be(newDisplayName);
    }
}
