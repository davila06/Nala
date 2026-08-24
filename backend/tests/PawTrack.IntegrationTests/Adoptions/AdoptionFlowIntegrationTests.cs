using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Adoptions;

[Collection("Integration")]
public sealed class AdoptionFlowIntegrationTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> LoginAsAdminAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@pawtrack.cr",
            password = "AdminPass1!",
        });
        if (!loginResp.IsSuccessStatusCode) return string.Empty;
        var body = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        return body?.AccessToken ?? string.Empty;
    }

    private static void SetBearer(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // ── Public directory ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAnimals_PublicEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/api/adoptions/animals");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnimals_WithFilters_Returns200()
    {
        var response = await _client.GetAsync("/api/adoptions/animals?species=Dog&size=Medium&page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnimalsForMap_Returns200()
    {
        var response = await _client.GetAsync("/api/adoptions/animals/map");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFairs_PublicEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/api/adoptions/fairs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnimal_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/adoptions/animals/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Auth guards ───────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAnimal_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/adoptions/animals", new
        {
            name = "Max",
            species = "Dog",
            size = "Medium",
            ageCategory = "Young",
            story = "Muy juguetón",
            refLat = 9.93,
            refLng = -84.08,
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApplyToAdopt_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync($"/api/adoptions/animals/{Guid.NewGuid()}/apply",
            new { note = "Quiero adoptarlo" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyApplications_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/adoptions/applications/mine");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Admin audit endpoint ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAuditLog_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/admin/audit");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdoptionAdminStats_Anonymous_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/admin/adoptions/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Pagination ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAnimals_PageSizeExceeded_ClampedTo50()
    {
        var response = await _client.GetAsync("/api/adoptions/animals?pageSize=999");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedBody>();
        body?.PageSize.Should().BeLessThanOrEqualTo(50);
    }

    // ── Supporting record types ───────────────────────────────────────────────

    private sealed record LoginResponse(string AccessToken);
    private sealed record PagedBody(int PageSize, int TotalCount);
}
