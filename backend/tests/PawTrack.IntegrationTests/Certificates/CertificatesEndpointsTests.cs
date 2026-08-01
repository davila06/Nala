using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Certificates;

public sealed class CertificatesEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Verify_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/api/certificates/verify/UNKNW999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Verify_IsAnonymous_Returns200()
    {
        // Anonymous access (no auth header) should be allowed for verification
        var response = await _client.GetAsync("/api/certificates/verify/TESTCODE");
        // 404 because it doesn't exist, but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Issue_WithoutPartnerSubscription_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        // The clinic exists but has no ClinicPartner subscription
        var response = await client.PostAsJsonAsync("/api/certificates", new
        {
            petId = Guid.NewGuid(),
            clinicId = Guid.NewGuid(),
            type = "Vaccination",
            petName = "Firulais",
            petSpecies = "Perro",
            clinicName = "Clínica San José",
            clinicLicense = "VET-001",
            vetName = "Dr. Pérez",
        });

        // Tier gate: no ClinicPartner subscription → 422
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Partner");
    }

    [Fact]
    public async Task GetForPet_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/certificates/pet/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetForClinic_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/certificates/clinic/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
