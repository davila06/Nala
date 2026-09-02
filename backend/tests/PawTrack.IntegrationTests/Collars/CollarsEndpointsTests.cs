using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
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

    // ── BOLA regression: RecordLocation must reject non-owners ──────────────
    // Found during E2E validation (2026-09-02): this endpoint had no ownership
    // check at all, so any authenticated user could push a fake GPS location
    // for any pet's collar. See docs/pendientesTotales.md §3.1.
    [Fact]
    public async Task RecordLocation_Owner_Returns204()
    {
        const string ownerEmail = "record_location_owner@pawtrack.cr";
        var ownerClient = await AuthHelper.CreatePlusClientAsync(factory, ownerEmail);
        var petId = await CreatePetWithCollarAsync(ownerEmail);

        var response = await ownerClient.PostAsJsonAsync($"/api/collars/pet/{petId}/location", new
        {
            lat = 9.9,
            lng = -84.1,
            batteryPercent = (int?)null,
            accuracy = (int?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RecordLocation_NonOwner_Returns403()
    {
        const string ownerEmail = "record_location_owner2@pawtrack.cr";
        const string attackerEmail = "record_location_attacker@pawtrack.cr";
        await AuthHelper.CreatePlusClientAsync(factory, ownerEmail);
        var petId = await CreatePetWithCollarAsync(ownerEmail);
        var attackerClient = await AuthHelper.CreatePlusClientAsync(factory, attackerEmail);

        var response = await attackerClient.PostAsJsonAsync($"/api/collars/pet/{petId}/location", new
        {
            lat = 0.0,
            lng = 0.0,
            batteryPercent = (int?)null,
            accuracy = (int?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Guid> CreatePetWithCollarAsync(string ownerEmail)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == ownerEmail);

        var pet = PawTrack.Domain.Pets.Pet.Create(user.Id, "LocationTestDog", PawTrack.Domain.Pets.PetSpecies.Dog, null, null);
        await db.Pets.AddAsync(pet);

        var collar = Collar.Register(pet.Id, user.Id, CollarProvider.Generic, "IMEI-LOC-001");
        await db.Collars.AddAsync(collar);
        await db.SaveChangesAsync();

        return pet.Id;
    }
}
