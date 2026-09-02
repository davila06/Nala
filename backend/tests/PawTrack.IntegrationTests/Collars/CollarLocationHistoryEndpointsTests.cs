using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Queries.GetCollarLocationHistoryRange;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end: activate collar → ingest a location → history/export/heatmap all reflect it.</summary>
[Collection("Integration")]
public sealed class CollarLocationHistoryEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private const string Serial = "PT-D9E0-0005555";

    [Fact]
    public async Task GetHistory_ExportCsv_AndHeatmap_ReflectIngestedPosition()
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
        form.Add(new StringContent("History Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();

        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{Serial}/activate", new { petId = petBody!.PetId });
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();

        var ingestClient = factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-Collar-Key", activateBody!.CollarApiKey);
        var ingestResp = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 9.91,
            lng = -84.09,
            batteryPercent = 77,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = 8,
        });
        ingestResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var historyResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/location-history");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var historyPoints = await historyResp.Content.ReadFromJsonAsync<List<CollarLocationPointDto>>();
        historyPoints.Should().Contain(p => p.Lat == 9.91 && p.Lng == -84.09);

        var csvResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/location-history/export.csv");
        csvResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvContent = await csvResp.Content.ReadAsStringAsync();
        csvContent.Should().Contain("lat,lng,accuracy_m,recorded_at");
        csvContent.Should().Contain("9.91");

        var heatmapResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/location-heatmap");
        heatmapResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var heatmapPoints = await heatmapResp.Content.ReadFromJsonAsync<List<CollarLocationPointDto>>();
        heatmapPoints.Should().Contain(p => p.Lat == 9.91);
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
}
