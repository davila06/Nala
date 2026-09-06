using System.Net;
using FluentAssertions;
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
}
