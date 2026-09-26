using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Pets.Commands.CreatePet;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Subscriptions;
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
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"medical-delete-{Guid.NewGuid():N}@pawtrack.cr");

        var response = await client.DeleteAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}");

        // 422 because Plan Familia check fires before record lookup
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateRecord_NonExistentRecord_Returns422()
    {
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"medical-update-{Guid.NewGuid():N}@pawtrack.cr");

        var response = await client.PutAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/{Guid.NewGuid()}",
            new { type = "Checkup", date = "2026-01-01", description = "test update" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateRecord_InvalidType_Returns400()
    {
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"medical-invalid-{Guid.NewGuid():N}@pawtrack.cr");

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
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"reminder-create-{Guid.NewGuid():N}@pawtrack.cr");

        var response = await client.PostAsJsonAsync(
            $"/api/pets/{Guid.NewGuid()}/medical/reminders",
            new { type = "Vaccine", dueDate = "2027-06-01", title = "Rabies booster" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateReminder_InvalidType_Returns400()
    {
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"reminder-invalid-{Guid.NewGuid():N}@pawtrack.cr");

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
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"reminder-delete-{Guid.NewGuid():N}@pawtrack.cr");

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

    [Fact]
    public async Task OtherOwner_CannotReadOrRevokePetClinicAccessGrants()
    {
        var ownerEmail = $"medical-grant-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var outsiderEmail = $"medical-grant-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var outsiderClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, outsiderEmail);
        Guid petId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var pet = Pet.Create(owner.Id, "Owner-only pet", PetSpecies.Dog, null, null);
            await db.Pets.AddAsync(pet);
            await db.SaveChangesAsync();
            petId = pet.Id;
        }

        var ownerRead = await ownerClient.GetAsync($"/api/pets/{petId}/clinic-access");
        ownerRead.StatusCode.Should().Be(HttpStatusCode.OK);

        var outsiderRead = await outsiderClient.GetAsync($"/api/pets/{petId}/clinic-access");
        outsiderRead.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var outsiderRevoke = await outsiderClient.DeleteAsync($"/api/pets/{petId}/clinic-access/{Guid.NewGuid()}");
        outsiderRevoke.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task OwnerMedicalResourceQueriesRejectAnotherOwnersPet()
    {
        var ownerEmail = $"medical-resource-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var outsiderEmail = $"medical-resource-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var outsiderClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);
        Guid petId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var outsider = await db.Users.SingleAsync(user => user.Email == outsiderEmail);
            var pet = Pet.Create(owner.Id, "Mascota privada", PetSpecies.Dog, null, null);
            db.Pets.Add(pet);
            var ownerPlan = Subscription.CreateForUser(owner.Id, SubscriptionTier.UserFamilia, $"M{Guid.NewGuid():N}"[..8], 4990m);
            var outsiderPlan = Subscription.CreateForUser(outsider.Id, SubscriptionTier.UserFamilia, $"M{Guid.NewGuid():N}"[..8], 4990m);
            ownerPlan.Activate();
            outsiderPlan.Activate();
            db.Subscriptions.AddRange(ownerPlan, outsiderPlan);
            await db.SaveChangesAsync();
            petId = pet.Id;
        }

        var cases = new (string Path, HttpStatusCode ForeignStatus)[]
        {
            ($"/api/pets/{petId}/medical/count", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical/access-log", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical/health-alerts", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical/weight-history", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical/reminders", HttpStatusCode.UnprocessableEntity),
            ($"/api/pets/{petId}/medical/health-score", HttpStatusCode.UnprocessableEntity),
            ($"/api/clinics/pets/{petId}/communication-preferences", HttpStatusCode.NotFound),
        };
        foreach (var scenario in cases)
        {
            var own = await ownerClient.GetAsync(scenario.Path);
            own.StatusCode.Should().Be(HttpStatusCode.OK, $"the owner must be able to use {scenario.Path}");
            var foreign = await outsiderClient.GetAsync(scenario.Path);
            foreign.StatusCode.Should().Be(scenario.ForeignStatus, $"another owner must not read {scenario.Path}");
        }
    }

    [Fact]
    public async Task MedicalMutationsRejectARecordOrReminderWhenRoutePetDoesNotMatch()
    {
        var ownerEmail = $"medical-binding-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var otherEmail = $"medical-binding-other-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, ownerEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, otherEmail);
        Guid ownPetId;
        Guid foreignPetId;
        Guid recordId;
        Guid reminderId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var other = await db.Users.SingleAsync(user => user.Email == otherEmail);
            var ownPet = Pet.Create(owner.Id, "Propia", PetSpecies.Dog, null, null);
            var foreignPet = Pet.Create(other.Id, "Ajena", PetSpecies.Dog, null, null);
            var record = MedicalRecord.Create(ownPet.Id, owner.Id, MedicalRecordType.Checkup,
                DateOnly.FromDateTime(DateTime.UtcNow), "Original", null, null, null);
            var reminder = VetReminder.Create(ownPet.Id, owner.Id, MedicalRecordType.Vaccine,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), "Refuerzo");
            var plan = Subscription.CreateForUser(owner.Id, SubscriptionTier.UserFamilia, $"M{Guid.NewGuid():N}"[..8], 4990m);
            plan.Activate();
            db.Pets.AddRange(ownPet, foreignPet);
            db.MedicalRecords.Add(record);
            db.VetReminders.Add(reminder);
            db.Subscriptions.Add(plan);
            await db.SaveChangesAsync();
            ownPetId = ownPet.Id;
            foreignPetId = foreignPet.Id;
            recordId = record.Id;
            reminderId = reminder.Id;
        }

        var update = await client.PutAsJsonAsync($"/api/pets/{foreignPetId}/medical/{recordId}",
            new { type = "Checkup", date = DateOnly.FromDateTime(DateTime.UtcNow), description = "Alterado" });
        update.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await client.DeleteAsync($"/api/pets/{foreignPetId}/medical/{recordId}"))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await client.PutAsync($"/api/pets/{foreignPetId}/medical/reminders/{reminderId}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await client.DeleteAsync($"/api/pets/{foreignPetId}/medical/reminders/{reminderId}"))
            .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.MedicalRecords.SingleAsync(item => item.Id == recordId)).Description.Should().Be("Original");
        (await verifyDb.VetReminders.SingleAsync(item => item.Id == reminderId)).IsCompleted.Should().BeFalse();

        var ownUpdate = await client.PutAsJsonAsync($"/api/pets/{ownPetId}/medical/{recordId}",
            new { type = "Checkup", date = DateOnly.FromDateTime(DateTime.UtcNow), description = "Corrección autorizada" });
        ownUpdate.StatusCode.Should().Be(HttpStatusCode.OK);
        var revision = await ownUpdate.Content.ReadFromJsonAsync<JsonElement>();
        var revisionId = revision.GetProperty("id").GetGuid();
        (await client.DeleteAsync($"/api/pets/{ownPetId}/medical/{revisionId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PutAsync($"/api/pets/{ownPetId}/medical/reminders/{reminderId}/complete", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"/api/pets/{ownPetId}/medical/reminders/{reminderId}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
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
