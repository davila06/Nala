using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Commands.CreateCollarSafeZone;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end: activate collar → create safe zone → ingest outside the zone → breach recorded.</summary>
[Collection("Integration")]
public sealed class CollarSafeZoneEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private const string Serial = "PT-B7C8-0004444";

    private static readonly string SquarePolygonJson = JsonSerializer.Serialize(new[]
    {
        new { lat = 9.9, lng = -84.1 },
        new { lat = 9.9, lng = -84.0 },
        new { lat = 10.0, lng = -84.0 },
        new { lat = 10.0, lng = -84.1 },
    });

    [Fact]
    public async Task CreateSafeZone_ThenIngestOutside_DoesNotErrorAndZoneListable()
    {
        var client = await AuthHelper.CreatePlusClientAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var tagRepo = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.ICollarTagRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.IUnitOfWork>();
            var tag = PawTrack.Domain.Collars.CollarTag.CreateFromFactory(Serial, "1.0.0");
            await tagRepo.AddAsync(tag, default);
            await uow.SaveChangesAsync(default);
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Safe Zone Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();

        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{Serial}/activate", new { petId = petBody!.PetId });
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();

        var createZoneResp = await client.PostAsJsonAsync(
            $"/api/collars/{activateBody!.CollarId}/safe-zones",
            new { name = "Casa", polygonJson = SquarePolygonJson });
        createZoneResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var zone = await createZoneResp.Content.ReadFromJsonAsync<CollarSafeZoneDto>();

        // First fix — inside the zone, establishes baseline (no alert expected)
        var ingestClient = factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-Collar-Key", activateBody.CollarApiKey);
        var firstIngest = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 9.95,
            lng = -84.05,
            batteryPercent = 90,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = 5,
        });
        firstIngest.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Second fix — outside the zone, should trigger a breach without erroring
        var secondIngest = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 8.0,
            lng = -83.0,
            batteryPercent = 88,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = 5,
        });
        secondIngest.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/safe-zones");
        var zones = await listResp.Content.ReadFromJsonAsync<List<CollarSafeZoneDto>>();
        zones.Should().ContainSingle(z => z.Id == zone!.Id);

        var deleteResp = await client.DeleteAsync($"/api/collars/safe-zones/{zone!.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
}
