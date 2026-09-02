using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.Application.Collars.Queries.GetCollarConnectivityStatus;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>
/// End-to-end coverage for the connectivity-alert preferences flow:
/// register collar → update notification preferences → read connectivity status.
/// </summary>
[Collection("Integration")]
public sealed class CollarConnectivityEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private static async Task<(HttpClient Client, Guid PetId, Guid CollarId)> RegisterCollarAsync(PawTrackWebApplicationFactory factory)
    {
        var client = await AuthHelper.CreatePlusClientAsync(factory);

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Connectivity Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        createPetResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();
        var petId = petBody!.PetId;

        var registerResp = await client.PostAsJsonAsync("/api/collars", new
        {
            petId,
            provider = "Generic",
            externalDeviceId = "GEN-CONN-001",
        });
        registerResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var collar = await registerResp.Content.ReadFromJsonAsync<CollarRegisterResultDto>();

        return (client, petId, collar!.Id);
    }

    [Fact]
    public async Task UpdatePreferences_ThenGetConnectivityStatus_ReflectsNewValues()
    {
        var (client, _, collarId) = await RegisterCollarAsync(factory);

        var updateResp = await client.PutAsJsonAsync($"/api/collars/{collarId}/notification-preferences", new
        {
            offlineAlertsEnabled = true,
            offlineThresholdMinutes = 45,
            batteryAlertsEnabled = true,
            batteryAlertThresholdPercent = 30,
        });
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResp = await client.GetAsync($"/api/collars/{collarId}/connectivity-status");
        statusResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await statusResp.Content.ReadFromJsonAsync<CollarConnectivityStatusDto>();

        status!.OfflineThresholdMinutes.Should().Be(45);
        status.BatteryAlertThresholdPercent.Should().Be(30);
        status.IsOffline.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePreferences_InvalidThreshold_Returns422()
    {
        var (client, _, collarId) = await RegisterCollarAsync(factory);

        var updateResp = await client.PutAsJsonAsync($"/api/collars/{collarId}/notification-preferences", new
        {
            offlineAlertsEnabled = true,
            offlineThresholdMinutes = 5, // below the 15-minute minimum
            batteryAlertsEnabled = true,
            batteryAlertThresholdPercent = 20,
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetConnectivityStatus_OtherUsersCollar_Returns403()
    {
        var (_, _, collarId) = await RegisterCollarAsync(factory);
        var otherClient = await AuthHelper.CreatePlusClientAsync(factory);

        var response = await otherClient.GetAsync($"/api/collars/{collarId}/connectivity-status");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record CollarRegisterResultDto(Guid Id);
}
