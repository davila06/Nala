using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

[Collection("Integration")]
public sealed class CollarsEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetStatus_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatus_AuthenticatedNonOwnedPet_Returns403Or404()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}");
        // Pet doesn't exist or doesn't belong to this user — either outcome is correct
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHistory_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}/history?hours=24");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_AuthenticatedNonOwnedPet_Returns403Or404()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}/history?hours=24");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/collars", new
        {
            petId = Guid.NewGuid(),
            provider = "Tractive",
            externalDeviceId = "TRC-001",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_EmptyPetId_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/collars", new
        {
            petId = Guid.Empty,
            provider = "Generic",
            externalDeviceId = (string?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }
}
