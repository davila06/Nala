using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Subscriptions;

[Collection("Integration")]
public sealed class SubscriptionsEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetMine_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/subscriptions/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/subscriptions", new
        {
            tier = "UserPlus",
            clinicId = (Guid?)null,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_InvalidTier_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/subscriptions", new
        {
            tier = "FakeTier",
            clinicId = (Guid?)null,
        });

        // FluentValidation or model binding should reject unknown tier
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancel_UnknownId_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMine_NewUser_ReturnsNullOrNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/subscriptions/me");

        // Ok(null) in ASP.NET returns 204 NoContent
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }
}
