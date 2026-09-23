using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Pets;

[Collection("Integration")]
public sealed class ClinicPetOwnershipTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task OwnerCannotReadUpdateOrDeleteAnotherOwnersPet()
    {
        var owner = await AuthHelper.CreateAuthenticatedClientAsync(factory, $"owner-a-{Guid.NewGuid():N}@pawtrack.cr");
        var otherOwner = await AuthHelper.CreateAuthenticatedClientAsync(factory, $"owner-b-{Guid.NewGuid():N}@pawtrack.cr");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Private pet"), "name");
        form.Add(new StringContent("Dog"), "species");
        var created = await owner.PostAsync("/api/pets", form);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await created.Content.ReadFromJsonAsync<PetCreatedResponse>();
        payload.Should().NotBeNull();

        var get = await otherOwner.GetAsync($"/api/pets/{payload!.PetId}");
        get.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        using var updateForm = new MultipartFormDataContent();
        updateForm.Add(new StringContent("Attempted takeover"), "name");
        updateForm.Add(new StringContent("Dog"), "species");
        var update = await otherOwner.PutAsync($"/api/pets/{payload.PetId}", updateForm);
        update.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);

        var delete = await otherOwner.DeleteAsync($"/api/pets/{payload.PetId}");
        delete.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClinicCannotRegisterPetAsOwner()
    {
        var email = $"clinic-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            user.AssignClinicRole();
            await db.SaveChangesAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer", jwt.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role));
        }

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Clinic-owned pet"), "name");
        form.Add(new StringContent("Dog"), "species");

        var response = await client.PostAsync("/api/pets", form);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using var scopeAfter = factory.Services.CreateScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await dbAfter.Pets.AnyAsync(p => p.Name == "Clinic-owned pet")).Should().BeFalse();
    }

    private sealed record PetCreatedResponse(Guid PetId);
}
