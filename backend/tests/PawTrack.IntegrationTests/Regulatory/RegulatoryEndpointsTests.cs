using System.Net;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Regulatory;

[Collection("Integration")]
public sealed class RegulatoryEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task PublicImpactStats_IsAnonymousAndReturnsNonUnauthorized()
    {
        var response = await client.GetAsync(
            "/api/public/impact-stats?periodStart=2026-01-01&periodEnd=2026-01-31");

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InstitutionalReports_WithoutAuthentication_Returns401()
    {
        var response = await client.GetAsync("/api/institutional/reports/catalog");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task NalaMap_WithoutAuthentication_Returns401()
    {
        var response = await client.GetAsync(
            "/api/nala/map-layers?south=8&north=11&west=-86&east=-82");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
