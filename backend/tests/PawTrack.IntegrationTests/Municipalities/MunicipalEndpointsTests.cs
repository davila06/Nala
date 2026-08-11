using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Municipalities;

[Collection("Integration")]
public sealed class MunicipalEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Search_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/municipalities/captures");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_Authenticated_Returns200WithEmptyPage()
    {
        var client = await AuthHelper.CreateMunicipalityClientAsync(factory);

        var response = await client.GetAsync("/api/municipalities/captures");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"items\"");
        body.Should().Contain("\"total\"");
    }

    [Fact]
    public async Task Record_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/municipalities/captures", new
        {
            canton = "Desamparados",
            species = "Perro",
            color = "Negro",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Record_MissingRequiredFields_Returns422()
    {
        var client = await AuthHelper.CreateMunicipalityClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/municipalities/captures", new
        {
            canton = "",   // required
            species = "",   // required
            color = "",   // required
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Record_ValidPayload_Returns201()
    {
        var client = await AuthHelper.CreateMunicipalityClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/municipalities/captures", new
        {
            canton = "Desamparados",
            species = "Perro",
            color = "Negro con blanco",
            breed = "Labrador",
            notes = "Muy manso",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
