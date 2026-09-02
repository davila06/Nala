using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Collars.Queries.GetCollarAuditLog;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end coverage for the owner-facing audit log: activate → audit entry recorded → readable via API.</summary>
[Collection("Integration")]
public sealed class CollarAuditLogEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private const string Serial = "PT-C4D5-0009999";

    [Fact]
    public async Task ActivateThenDeactivate_AuditLogContainsBothEvents()
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
        form.Add(new StringContent("Audit Dog"), "name");
        form.Add(new StringContent("Dog"), "species");
        var createPetResp = await client.PostAsync("/api/pets", form);
        var petBody = await createPetResp.Content.ReadFromJsonAsync<PetCreateResultDto>();

        var activateResp = await client.PostAsJsonAsync(
            $"/api/collars/tag/{Serial}/activate", new { petId = petBody!.PetId });
        activateResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var activateBody = await activateResp.Content.ReadFromJsonAsync<ActivateResultDto>();

        var deactivateResp = await client.DeleteAsync($"/api/collars/tag/{Serial}/deactivate");
        deactivateResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var auditResp = await client.GetAsync($"/api/collars/{activateBody!.CollarId}/audit-log");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var entries = await auditResp.Content.ReadFromJsonAsync<List<CollarAuditEntryDto>>();

        entries.Should().Contain(e => e.Event == "Activated");
        entries.Should().Contain(e => e.Event == "Deactivated");
    }

    private sealed record PetCreateResultDto(Guid PetId);
    private sealed record ActivateResultDto(Guid CollarId, string Serial, string CollarApiKey);
}
