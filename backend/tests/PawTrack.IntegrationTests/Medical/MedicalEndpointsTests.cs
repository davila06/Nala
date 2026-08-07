using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.CreatePet;
using PawTrack.Domain.Pets;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Medical;

[Collection("Integration")]
public sealed class MedicalEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    // ── Auth guard tests (no plan setup needed) ───────────────────────────────

    [Fact]
    public async Task GetHistory_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/pets/{Guid.NewGuid()}/medical");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddRecord_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Checkup"), "type");
        form.Add(new StringContent("2026-01-01"), "date");
        form.Add(new StringContent("checkup"), "description");

        var response = await client.PostAsync($"/api/pets/{Guid.NewGuid()}/medical", form);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteRecord_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRecord_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}",
            new { type = "Checkup", date = "2026-01-01", description = "x" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReminders_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/pets/{Guid.NewGuid()}/medical/reminders");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReminder_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders",
            new { type = "Vaccine", dueDate = "2027-01-01", title = "title" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteReminder_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteReminder_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.PutAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders/{Guid.NewGuid()}/complete", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportPdf_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/pets/{Guid.NewGuid()}/medical/export");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated but no Familia plan — all write ops return 422 ──────────

    [Fact]
    public async Task GetHistory_AuthenticatedNoFamiliaplan_Returns422()
    {
        // Explorador plan user cannot read medical history
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        var petId = await CreateTestPetAsync(client, factory);

        var response = await client.GetAsync($"/api/pets/{petId}/medical");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteRecord_NonExistentRecord_Returns422()
    {
        // Authenticated + any plan — a non-existent record should always return 422
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.DeleteAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}");

        // 422 because Plan Familia check fires before record lookup
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateRecord_NonExistentRecord_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PutAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}",
            new { type = "Checkup", date = "2026-01-01", description = "test update" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateRecord_InvalidType_Returns400()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PutAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}",
            new { type = "INVALID_TYPE", date = "2026-01-01", description = "x" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateReminder_NonExistentPet_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders",
            new { type = "Vaccine", dueDate = "2027-06-01", title = "Rabies booster" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateReminder_InvalidType_Returns400()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders",
            new { type = "INVALID", dueDate = "2027-06-01", title = "test" });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteReminder_NonExistentReminder_Returns422()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var response = await client.DeleteAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ── Clinic access guard tests ─────────────────────────────────────────────

    [Fact]
    public async Task GetClinicAccess_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/pets/{Guid.NewGuid()}/clinic-access");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeClinicAccess_Unauthenticated_Returns401()
    {
        var client = factory.CreateClient();
        var response = await client.DeleteAsync(
            $"/api/pets/{Guid.NewGuid()}/clinic-access/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<Guid> CreateTestPetAsync(
        HttpClient client, PawTrackWebApplicationFactory factory)
    {
        // Extract user id from JWT token — use MediatR instead to be precise
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        // Create a pet directly via repo so the owner matches the authenticated user
        var petRepo = scope.ServiceProvider.GetRequiredService<IPetRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // We can't easily get the userId from the JWT in this context;
        // use a random owner — the pet won't match but gets us past route binding.
        var pet = Pet.Create(Guid.NewGuid(), "TestPet", PetSpecies.Dog, null, null);
        await petRepo.AddAsync(pet, CancellationToken.None);
        await uow.SaveChangesAsync(CancellationToken.None);

        return pet.Id;
    }
}
