using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Commands.ActivateCollarLostMode;
using PawTrack.Application.Collars.Queries.GetCollarLostModeStatus;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end: activate collar → activate lost mode (auto-creates LostPetEvent) → ingest updates it → deactivate.</summary>
[Collection("Integration")]
public sealed class CollarLostModeEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private const string Serial = "PT-A1B2-0003333";

    [Fact]
    public async Task ActivateLostMode_AutoCreatesLostPetEventAndSyncsPosition()
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
        form.Add(new StringContent("Lost Mode Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();

        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{Serial}/activate", new { petId = petBody!.PetId });
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();

        // Activate lost mode
        var lostModeResp = await client.PostAsync($"/api/collars/{activateBody!.CollarId}/lost-mode/activate", null);
        lostModeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var lostModeBody = await lostModeResp.Content.ReadFromJsonAsync<ActivateCollarLostModeResultDto>();
        lostModeBody!.WasNewlyCreated.Should().BeTrue();

        var statusResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/lost-mode-status");
        var statusBody = await statusResp.Content.ReadFromJsonAsync<CollarLostModeStatusDto>();
        statusBody!.IsLost.Should().BeTrue();
        statusBody.LostPetEventId.Should().Be(lostModeBody.LostPetEventId);

        // Ingest a new position while lost — should sync into the LostPetEvent
        var ingestClient = factory.CreateClient();
        ingestClient.DefaultRequestHeaders.Add("X-Collar-Key", activateBody.CollarApiKey);
        var ingestResp = await ingestClient.PostAsJsonAsync("/api/collars/ingest", new
        {
            serial = Serial,
            lat = 9.93,
            lng = -84.08,
            batteryPercent = 40,
            timestamp = DateTimeOffset.UtcNow,
            accuracyMeters = 5,
        });
        ingestResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Deactivate lost mode
        var deactivateResp = await client.PostAsync($"/api/collars/{activateBody.CollarId}/lost-mode/deactivate", null);
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var finalStatusResp = await client.GetAsync($"/api/collars/{activateBody.CollarId}/lost-mode-status");
        var finalStatusBody = await finalStatusResp.Content.ReadFromJsonAsync<CollarLostModeStatusDto>();
        finalStatusBody!.IsLost.Should().BeFalse();
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
}
