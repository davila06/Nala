using System.Net;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Subscriptions;

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
}
