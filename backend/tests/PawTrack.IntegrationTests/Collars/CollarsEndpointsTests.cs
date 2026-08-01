using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

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
    public async Task GetStatus_AuthenticatedNoCollar_Returns200OrNoContent()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}");
        // Ok(null) returns 204; or 200 with null body
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetHistory_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}/history?hours=24");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetHistory_AuthenticatedNoCollar_Returns200EmptyArray()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var response = await client.GetAsync($"/api/collars/pet/{Guid.NewGuid()}/history?hours=24");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<object[]>();
        items.Should().BeEmpty();
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
