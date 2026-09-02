using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Commands.GenerateCollarHandoverCode;
using PawTrack.Application.Collars.Commands.RedeemCollarHandoverCode;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end coverage: activate → generate handover code → redeem → serial releases for reactivation.</summary>
[Collection("Integration")]
public sealed class CollarHandoverEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private async Task<(Guid CollarId, HttpClient OldOwnerClient)> ActivateCollarAsync(string serial)
    {
        var client = await AuthHelper.CreatePlusClientAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var tagRepo = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.ICollarTagRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.IUnitOfWork>();
            var tag = PawTrack.Domain.Collars.CollarTag.CreateFromFactory(serial, "1.0.0");
            await tagRepo.AddAsync(tag, default);
            await uow.SaveChangesAsync(default);
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Handover Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();

        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{serial}/activate", new { petId = petBody!.PetId });
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();

        return (activateBody!.CollarId, client);
    }

    [Fact]
    public async Task GenerateThenRedeem_HappyPath_ReleasesSerial()
    {
        const string serial = "PT-E6F7-0001111";
        var (collarId, oldOwnerClient) = await ActivateCollarAsync(serial);

        var generateResp = await oldOwnerClient.PostAsync($"/api/collars/{collarId}/handover/generate", null);
        generateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var generateBody = await generateResp.Content.ReadFromJsonAsync<GenerateCollarHandoverCodeResultDto>();

        var newOwnerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var redeemResp = await newOwnerClient.PostAsJsonAsync("/api/collars/handover/redeem", new
        {
            handoverCodeId = generateBody!.HandoverCodeId,
            pin = generateBody.Pin,
        });

        redeemResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var redeemBody = await redeemResp.Content.ReadFromJsonAsync<RedeemCollarHandoverCodeResultDto>();
        redeemBody!.Serial.Should().Be(serial);

        // Serial is available again for reactivation
        var checkResp = await newOwnerClient.GetAsync($"/api/collars/tag/{serial}");
        var checkBody = await checkResp.Content.ReadFromJsonAsync<SerialStatusDto>();
        checkBody!.Available.Should().BeTrue();
    }

    [Fact]
    public async Task Redeem_WrongPin_ReturnsFailureWithRemainingAttempts()
    {
        const string serial = "PT-E6F7-0002222";
        var (collarId, oldOwnerClient) = await ActivateCollarAsync(serial);
        var generateResp = await oldOwnerClient.PostAsync($"/api/collars/{collarId}/handover/generate", null);
        var generateBody = await generateResp.Content.ReadFromJsonAsync<GenerateCollarHandoverCodeResultDto>();

        var newOwnerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var redeemResp = await newOwnerClient.PostAsJsonAsync("/api/collars/handover/redeem", new
        {
            handoverCodeId = generateBody!.HandoverCodeId,
            pin = "000000",
        });

        redeemResp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
    private sealed record SerialStatusDto(bool Available, string Status);
}
