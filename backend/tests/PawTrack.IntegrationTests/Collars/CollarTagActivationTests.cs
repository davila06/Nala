using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Commands.Admin;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Pets;
using PawTrack.IntegrationTests.Infrastructure;
using MediatR;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>
/// Full activation + ingest flow: register serial → activate via API (JWT) →
/// ingest location via API (X-Collar-Key) → verify CollarLocations has the record.
/// </summary>
public sealed class CollarTagActivationTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private const string Serial = "PT-F1E2-0000001";

    [Fact]
    public async Task ActivateAndIngest_HappyPath_RecordsLocation()
    {
        // 1. Seed a CollarTag serial and a pet (Plus subscription needed)
        var client = await AuthHelper.CreatePlusClientAsync(factory);
        Guid petId;
        string collarApiKey;

        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var tagRepo = sp.GetRequiredService<ICollarTagRepository>();
            var uow = sp.GetRequiredService<IUnitOfWork>();

            var tag = CollarTag.CreateFromFactory(Serial, "1.0.0");
            await tagRepo.AddAsync(tag, default);
            await uow.SaveChangesAsync(default);

            // Create a pet via multipart form (PetsController uses [FromForm])
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("IntegrationDog"), "name");
            form.Add(new StringContent("Dog"), "species");
            var createPetResp = await client.PostAsync("/api/pets", form);
            createPetResp.StatusCode.Should().Be(HttpStatusCode.Created);
            var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();
            petId = petBody!.PetId;
        }

        // 2. Check serial availability
        var checkResp = await client.GetAsync($"/api/collars/tag/{Serial}");
        checkResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkBody = await checkResp.Content.ReadFromJsonAsync<SerialStatusDto>();
        checkBody!.Available.Should().BeTrue();

        // 3. Activate the serial → get raw key (shown once)
        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{Serial}/activate",
            new { petId });
        var activateBody422 = await activateResp.Content.ReadAsStringAsync();
        activateResp.StatusCode.Should().Be(HttpStatusCode.OK, activateBody422);
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();
        activateBody!.Serial.Should().Be(Serial);
        collarApiKey = activateBody.CollarApiKey;
        collarApiKey.Should().StartWith("ptwk_collar_");

        // 4. Ingest a location using the device key (no JWT)
        var ingestClient = factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-Collar-Key", collarApiKey);
        var ingestResp = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 9.9,
            lng = -84.1,
            batteryPercent = 72,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = 8,
        });
        ingestResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify a location point was recorded in the DB
        using (var scope = factory.Services.CreateScope())
        {
            var collarRepo = scope.ServiceProvider.GetRequiredService<ICollarRepository>();
            var collar = await collarRepo.GetActiveForPetAsync(petId, default);
            collar.Should().NotBeNull();
            collar!.LastLat.Should().Be(9.9);
            collar.BatteryPercent.Should().Be(72);
        }
    }

    [Fact]
    public async Task Ingest_WithoutKey_Returns401()
    {
        var ingestClient = factory.CreateClient();
        var ingestResp = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 9.9,
            lng = -84.1,
            batteryPercent = 100,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = (int?)null,
        });
        ingestResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GenerateKey_ThenIngest_ForGenericCollar()
    {
        const string userEmail = "generic_collar_test@pawtrack.cr";
        var client = await AuthHelper.CreatePlusClientAsync(factory, userEmail);
        Guid collarId;
        string deviceKey;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = await db.Users.FirstAsync(u => u.Email == userEmail);

            var pet = PawTrack.Domain.Pets.Pet.Create(user.Id, "OEMDog", PawTrack.Domain.Pets.PetSpecies.Dog, null, null);
            await db.Pets.AddAsync(pet);

            var collar = Collar.Register(pet.Id, user.Id, CollarProvider.Generic, "IMEI-OEM-001");
            await db.Collars.AddAsync(collar);
            await db.SaveChangesAsync();
            collarId = collar.Id;
        }

        // Generate device key for the Generic collar via API
        var keyResp = await client.PostAsync($"/api/collars/{collarId}/generate-key", null);
        keyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var keyBody = await keyResp.Content.ReadFromJsonAsync<DeviceKeyDto>();
        deviceKey = keyBody!.CollarDeviceKey;
        deviceKey.Should().StartWith("ptwk_collar_");

        // Verify the key is stored hashed and usable (credential exists in DB)
        using (var scope = factory.Services.CreateScope())
        {
            var credRepo = scope.ServiceProvider.GetRequiredService<ICollarDeviceCredentialRepository>();
            var hash = PawTrack.Application.Collars.CollarDeviceKeyHasher.Compute(deviceKey);
            var cred = await credRepo.GetActiveByHashAsync(hash, default);
            cred.Should().NotBeNull("the generated key must be persisted as a hashed credential");
            cred!.CollarId.Should().Be(collarId);
        }
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record SerialStatusDto(bool Available, string Status);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
    private sealed record DeviceKeyDto(Guid CollarId, string CollarDeviceKey);
}
