using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Bounties;

public sealed class BountiesEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetForEvent_UnknownEvent_Returns200OrNoContent()
    {
        var response = await _client.GetAsync($"/api/bounties/event/{Guid.NewGuid()}");
        // AllowAnonymous; Ok(null) returns 204 NoContent in ASP.NET when value is null
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/bounties", new
        {
            lostPetEventId = Guid.NewGuid(),
            amount = 25000m,
            currencyCode = "CRC",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AmountBelowMinimum_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/bounties", new
        {
            lostPetEventId = Guid.NewGuid(),
            amount = 100m, // below ₡5,000 minimum
            currencyCode = "CRC",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConfirmDeposit_UnknownReference_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PutAsJsonAsync("/api/bounties/confirm-deposit", new
        {
            depositReference = "UNKNWREF",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Release_UnknownId_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PutAsJsonAsync($"/api/bounties/{Guid.NewGuid()}/release", new { });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConfirmDeposit_Unauthenticated_RefusesNonOwnerConfirm()
    {
        // Create bounty as User A
        var clientA = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var createResp = await clientA.PostAsJsonAsync("/api/bounties", new
        {
            lostPetEventId = Guid.NewGuid(),
            amount = 25_000m,
            currencyCode = "CRC",
        });
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var bounty = await createResp.Content.ReadFromJsonAsync<BountyResponse>();
        bounty.Should().NotBeNull();

        // Try to confirm deposit as User B (different user)
        var clientB = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var confirmResp = await clientB.PutAsJsonAsync("/api/bounties/confirm-deposit", new
        {
            depositReference = bounty!.DepositReference,
        });

        // Must return 422 Access denied — not the owner
        confirmResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record BountyResponse(Guid Id, string DepositReference, string Status);
}
