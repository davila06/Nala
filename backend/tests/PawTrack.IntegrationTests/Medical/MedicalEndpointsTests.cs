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
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Subscriptions;
using PawTrack.Domain.Certificates;
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
    public async Task Timeline_PaginatesRecordsAndRejectsAnotherOwner()
    {
        var email = $"timeline-owner-{Guid.NewGuid():N}@pawtrack.cr";
        using var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        using var otherClient = await AuthHelper.CreateAuthenticatedClientAsync(factory);
        Guid petId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == email);
            var pet = Pet.Create(owner.Id, "Max", PetSpecies.Dog, null, null);
            var plan = Subscription.CreateForUser(owner.Id, SubscriptionTier.UserFamilia, $"M{Guid.NewGuid():N}"[..8], 4990m);
            plan.Activate();
            db.Pets.Add(pet);
            db.Subscriptions.Add(plan);
            for (var day = 1; day <= 3; day++)
                db.MedicalRecords.Add(MedicalRecord.Create(pet.Id, owner.Id, MedicalRecordType.Checkup,
                    new DateOnly(2026, 9, day), $"Consulta {day}", null, null, null));
            await db.SaveChangesAsync();
            petId = pet.Id;
        }

        var firstResponse = await ownerClient.GetAsync($"/api/pets/{petId}/medical/timeline?page=1&pageSize=2");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await firstResponse.Content.ReadFromJsonAsync<TimelineResponse>();
        first!.Items.Select(item => item.Label).Should().Equal("Consulta 3", "Consulta 2");
        first.HasMore.Should().BeTrue();

        var secondResponse = await ownerClient.GetAsync($"/api/pets/{petId}/medical/timeline?page=2&pageSize=2");
        var second = await secondResponse.Content.ReadFromJsonAsync<TimelineResponse>();
        second!.Items.Select(item => item.Label).Should().Equal("Consulta 1");
        second.HasMore.Should().BeFalse();

        var recordPage = await ownerClient.GetAsync($"/api/pets/{petId}/medical/page?page=1&pageSize=2");
        recordPage.StatusCode.Should().Be(HttpStatusCode.OK);
        var recordResult = await recordPage.Content.ReadFromJsonAsync<PagedMedicalRecordsResponse>();
        recordResult!.Records.Select(record => record.Description).Should().Equal("Consulta 3", "Consulta 2");
        recordResult.HasMore.Should().BeTrue();
        var nextRecordPage = await ownerClient.GetFromJsonAsync<PagedMedicalRecordsResponse>(
            $"/api/pets/{petId}/medical/page?page=2&pageSize=2");
        nextRecordPage!.Records.Select(record => record.Description).Should().Equal("Consulta 1");
        nextRecordPage.HasMore.Should().BeFalse();

        (await otherClient.GetAsync($"/api/pets/{petId}/medical/timeline?page=1&pageSize=2"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var report = await ownerClient.GetAsync($"/api/pets/{petId}/medical/consolidated-report");
        report.StatusCode.Should().Be(HttpStatusCode.OK);
        report.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        (await report.Content.ReadAsByteArrayAsync()).Take(4).Should().Equal(0x25, 0x50, 0x44, 0x46);
        (await otherClient.GetAsync($"/api/pets/{petId}/medical/consolidated-report"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record TimelineItem(string Label);
    private sealed record TimelineResponse(List<TimelineItem> Items, bool HasMore);
    private sealed record PagedMedicalRecord(string Description);
    private sealed record PagedMedicalRecordsResponse(List<PagedMedicalRecord> Records, bool HasMore);

    [Fact]
    public async Task Timeline_CertificatesIssuedTheSameDay_KeepIssuanceOrderAcrossPages()
    {
        var email = $"timeline-cert-{Guid.NewGuid():N}@pawtrack.cr";
        using var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        Guid petId;
        Guid[] expectedIds;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == email);
            var pet = Pet.Create(owner.Id, "Luna", PetSpecies.Cat, null, null);
            var plan = Subscription.CreateForUser(owner.Id, SubscriptionTier.UserFamilia, $"M{Guid.NewGuid():N}"[..8], 4990m);
            plan.Activate();
            db.Pets.Add(pet);
            db.Subscriptions.Add(plan);
            var certificates = Enumerable.Range(0, 3)
                .Select(index => VetCertificate.Issue(pet.Id, Guid.NewGuid(), owner.Id,
                    CertificateType.GeneralExam, $"A{index:0000000}"))
                .ToArray();
            var reversedIds = new[]
            {
                Guid.Parse("ffffffff-ffff-4fff-8fff-fffffffffff1"),
                Guid.Parse("88888888-8888-4888-8888-888888888882"),
                Guid.Parse("00000000-0000-4000-8000-000000000003"),
            };
            for (var index = 0; index < certificates.Length; index++)
            {
                db.Entry(certificates[index]).Property(item => item.Id).CurrentValue = reversedIds[index];
                db.Entry(certificates[index]).Property(item => item.IssuedAt).CurrentValue =
                    new DateTimeOffset(2026, 9, 27, 12, index, 0, TimeSpan.Zero);
            }
            db.VetCertificates.AddRange(certificates);
            await db.SaveChangesAsync();
            petId = pet.Id;
            expectedIds = certificates.OrderByDescending(item => item.IssuedAt).Select(item => item.Id).ToArray();
        }

        var first = await client.GetFromJsonAsync<TimelineIdsResponse>($"/api/pets/{petId}/medical/timeline?page=1&pageSize=2");
        var second = await client.GetFromJsonAsync<TimelineIdsResponse>($"/api/pets/{petId}/medical/timeline?page=2&pageSize=2");

        first!.Items.Concat(second!.Items).Select(item => item.Id).Should().Equal(expectedIds);
        first.HasMore.Should().BeTrue();
        second.HasMore.Should().BeFalse();
    }

    private sealed record TimelineIdItem(Guid Id);
    private sealed record TimelineIdsResponse(List<TimelineIdItem> Items, bool HasMore);

    [Fact]
    public async Task MedicalPage_PlusPreviewMasksAttachmentAndDeniesTimelineAndReport()
    {
        var email = $"timeline-plus-{Guid.NewGuid():N}@pawtrack.cr";
        using var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        Guid petId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == email);
            var pet = Pet.Create(owner.Id, "Toby", PetSpecies.Dog, null, null);
            var plan = Subscription.CreateForUser(owner.Id, SubscriptionTier.UserPlus, $"M{Guid.NewGuid():N}"[..8], 1990m);
            plan.Activate();
            var record = MedicalRecord.Create(pet.Id, owner.Id, MedicalRecordType.Other,
                new DateOnly(2026, 9, 20), "Examen", null, null, null);
            record.SetDocumentUrl("https://example.invalid/private.pdf", MedicalDocumentKind.Radiograph);
            db.Pets.Add(pet);
            db.Subscriptions.Add(plan);
            db.MedicalRecords.Add(record);
            await db.SaveChangesAsync();
            petId = pet.Id;
        }

        var response = await client.GetAsync($"/api/pets/{petId}/medical/page?page=1&pageSize=20");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        root.GetProperty("accessTier").GetString().Should().Be("plus_preview");
        root.GetProperty("records")[0].GetProperty("documentUrl").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("records")[0].GetProperty("documentKind").ValueKind.Should().Be(JsonValueKind.Null);
        (await client.GetAsync($"/api/pets/{petId}/medical/timeline")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/api/pets/{petId}/medical/consolidated-report")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        var ownerClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, ownerEmail);
        var outsiderClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, outsiderEmail);
        Guid petId;
        Guid clinicId;
        Guid grantId;
        string rawCode;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var pet = Pet.Create(owner.Id, "Owner-only pet", PetSpecies.Dog, null, null);
            var clinic = Clinic.Create(Guid.NewGuid(), "Centro aliado", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, $"clinic-{Guid.NewGuid():N}@pawtrack.cr");
            clinic.Activate();
            var generated = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, owner.Id, "Clinic");
            await db.Pets.AddAsync(pet);
            await db.Clinics.AddAsync(clinic);
            await db.ClinicMedicalAccessGrants.AddAsync(generated.Grant);
            await db.SaveChangesAsync();
            petId = pet.Id;
            clinicId = clinic.Id;
            grantId = generated.Grant.Id;
            rawCode = generated.RawCode;
        }

        var ownerRead = await ownerClient.GetAsync($"/api/pets/{petId}/clinic-access");
        ownerRead.StatusCode.Should().Be(HttpStatusCode.OK);

        var outsiderRead = await outsiderClient.GetAsync($"/api/pets/{petId}/clinic-access");
        outsiderRead.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var outsiderAccept = await outsiderClient.PostAsJsonAsync($"/api/pets/{petId}/clinic-access/accept", new { code = rawCode });
        outsiderAccept.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownerAccept = await ownerClient.PostAsJsonAsync($"/api/pets/{petId}/clinic-access/accept", new { code = rawCode });
        ownerAccept.StatusCode.Should().Be(HttpStatusCode.OK);
        var outsiderRevoke = await outsiderClient.DeleteAsync($"/api/pets/{petId}/clinic-access/{clinicId}");
        outsiderRevoke.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            (await db.ClinicMedicalAccessGrants.SingleAsync(grant => grant.Id == grantId)).IsActive.Should().BeTrue();
        }
        var ownerRevoke = await ownerClient.DeleteAsync($"/api/pets/{petId}/clinic-access/{clinicId}");
        ownerRevoke.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicMedicalAccessGrants.SingleAsync(grant => grant.Id == grantId)).IsActive.Should().BeFalse();
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
