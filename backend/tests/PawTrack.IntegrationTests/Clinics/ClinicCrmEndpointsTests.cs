using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicCrmEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task OwnerConsentAndClinicOptOut_ArePersistedAndClinicCannotOptIn()
    {
        var clinicEmail = $"clinic-crm-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"owner-crm-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid petId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Clinica CRM", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var pet = Pet.Create(owner.Id, "Max", PetSpecies.Dog, null, null);
            var vet = ClinicVeterinarian.Create(clinic.Id, "Dra. Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, vet.Id, pet.Id, DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
            await db.Clinics.AddAsync(clinic);
            await db.Pets.AddAsync(pet);
            await db.ClinicVeterinarians.AddAsync(vet);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            petId = pet.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true));
        }

        var forged = await clinicClient.PutAsJsonAsync("/api/clinics/me/crm/preferences", new
        {
            petId,
            channel = "Email",
            purpose = "ClinicalFollowUp",
            isOptedIn = true,
            consentSource = "reception"
        });
        forged.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var ownerPreferences = await ownerClient.GetAsync($"/api/clinics/pets/{petId}/communication-preferences");
        ownerPreferences.StatusCode.Should().Be(HttpStatusCode.OK);
        var availableClinics = await ownerPreferences.Content.ReadFromJsonAsync<List<OwnerClinicPreferenceResponse>>();
        availableClinics.Should().ContainSingle(clinicPreference => clinicPreference.ClinicId == clinicId);

        var consent = await ownerClient.PutAsJsonAsync($"/api/clinics/{clinicId}/pets/{petId}/communication-preferences", new
        {
            channel = "Email",
            purpose = "ClinicalFollowUp",
            isOptedIn = true
        });
        consent.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var otherOwner = await AuthHelper.CreateAuthenticatedClientAsync(factory, $"other-crm-{Guid.NewGuid():N}@pawtrack.cr");
        var forgedOwner = await otherOwner.PutAsJsonAsync($"/api/clinics/{clinicId}/pets/{petId}/communication-preferences", new
        {
            channel = "Email",
            purpose = "ClinicalFollowUp",
            isOptedIn = false
        });
        forgedOwner.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var dashboard = await clinicClient.GetAsync("/api/clinics/me/crm-dashboard");
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await dashboard.Content.ReadFromJsonAsync<CrmDashboardResponse>();
        data!.Preferences.Should().ContainSingle(preference => preference.PetId == petId && preference.IsOptedIn);

        var optOut = await clinicClient.PutAsJsonAsync("/api/clinics/me/crm/preferences", new
        {
            petId,
            channel = "Email",
            purpose = "ClinicalFollowUp",
            isOptedIn = false,
            consentSource = "owner request"
        });
        optOut.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedDashboard = await clinicClient.GetFromJsonAsync<CrmDashboardResponse>("/api/clinics/me/crm-dashboard");
        updatedDashboard!.Preferences.Should().ContainSingle(preference => preference.PetId == petId && !preference.IsOptedIn);
    }

    [Fact]
    public async Task ReceptionistStaffEndpoints_ReturnOnlyAuthorizedTasksAndRejectOtherRoles()
    {
        var ownerEmail = $"clinic-crm-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"clinic-crm-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var outsiderEmail = $"clinic-crm-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        var outsiderClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);
        Guid clinicId;
        Guid petId;
        Guid receptionTaskId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var staffUser = await db.Users.SingleAsync(user => user.Email == staffEmail);
            var clinic = Clinic.Create(owner.Id, "Clinica CRM Staff", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, ownerEmail);
            clinic.Activate();
            var pet = Pet.Create(owner.Id, "Nala", PetSpecies.Dog, null, null);
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddDays(1), TimeSpan.FromMinutes(30));
            var receptionTask = ClinicCrmTask.Create(clinic.Id, pet.Id, pet.OwnerId, ClinicCrmTaskType.ConfirmAppointment, new DateOnly(2026, 9, 25), "Confirmar cita", null, owner.Id);
            var veterinarianTask = ClinicCrmTask.Create(clinic.Id, pet.Id, pet.OwnerId, ClinicCrmTaskType.FollowUpTreatment, new DateOnly(2026, 9, 25), "Seguimiento clínico", "Dato clínico", owner.Id);
            var preference = ClinicClientCommunicationPreference.Create(clinic.Id, pet.Id, pet.OwnerId,
                ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, true, "Portal del tutor", pet.OwnerId);
            await db.Clinics.AddAsync(clinic);
            await db.Pets.AddAsync(pet);
            await db.ClinicVeterinarians.AddAsync(veterinarian);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.ClinicCrmTasks.AddRangeAsync(receptionTask, veterinarianTask);
            await db.ClinicClientCommunicationPreferences.AddAsync(preference);
            await db.ClinicStaffMemberships.AddAsync(ClinicStaffMembership.Grant(clinic.Id, staffUser.Id, ClinicStaffRole.Receptionist, owner.Id));
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            petId = pet.Id;
            receptionTaskId = receptionTask.Id;
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(staffUser.Id, staffUser.Email, staffUser.Name, staffUser.Role));
        }

        var dashboard = await staffClient.GetAsync($"/api/clinics/{clinicId}/staff/crm-dashboard?today=2026-09-25");
        dashboard.StatusCode.Should().Be(HttpStatusCode.OK);
        using (var payload = await System.Text.Json.JsonDocument.ParseAsync(await dashboard.Content.ReadAsStreamAsync()))
        {
            payload.RootElement.GetProperty("openTasks").EnumerateArray()
                .Select(task => task.GetProperty("type").GetString())
                .Should().ContainSingle(type => type == nameof(ClinicCrmTaskType.ConfirmAppointment));
            payload.RootElement.GetProperty("preferences").GetArrayLength().Should().Be(0);
            payload.RootElement.GetProperty("recentActivities").GetArrayLength().Should().Be(0);
            payload.RootElement.GetProperty("segments").GetArrayLength().Should().Be(0);
        }

        var idempotencyKey = Guid.NewGuid();
        var createPayload = new
        {
            petId,
            type = nameof(ClinicCrmTaskType.CallClient),
            dueDate = "2026-09-25",
            title = "Llamar al tutor",
            priority = nameof(ClinicCrmTaskPriority.High),
            idempotencyKey
        };
        var withoutMfa = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", createPayload);
        withoutMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staffUser = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(staffUser.Id, staffUser.Email, staffUser.Name, staffUser.Role, mfaVerified: true));
        }

        var wrongRoleTask = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", new
        {
            petId,
            type = nameof(ClinicCrmTaskType.FollowUpTreatment),
            dueDate = "2026-09-25",
            title = "No autorizado",
            priority = nameof(ClinicCrmTaskPriority.Normal),
            idempotencyKey = Guid.NewGuid()
        });
        wrongRoleTask.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var allowedTask = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", createPayload);
        allowedTask.StatusCode.Should().Be(HttpStatusCode.Created);
        using var createdPayload = await System.Text.Json.JsonDocument.ParseAsync(await allowedTask.Content.ReadAsStreamAsync());
        var createdTaskId = createdPayload.RootElement.GetProperty("taskId").GetGuid();

        var retry = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", createPayload);
        retry.StatusCode.Should().Be(HttpStatusCode.Created);
        using var retryPayload = await System.Text.Json.JsonDocument.ParseAsync(await retry.Content.ReadAsStreamAsync());
        retryPayload.RootElement.GetProperty("taskId").GetGuid().Should().Be(createdTaskId);

        var conflictingRetry = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", new
        {
            petId,
            type = nameof(ClinicCrmTaskType.CallClient),
            dueDate = "2026-09-25",
            title = "Payload diferente",
            priority = nameof(ClinicCrmTaskPriority.High),
            idempotencyKey
        });
        conflictingRetry.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            (await db.ClinicCrmTasks.CountAsync(task => task.ClinicId == clinicId && task.IdempotencyKey == idempotencyKey)).Should().Be(1);
        }

        var complete = await staffClient.PostAsync($"/api/clinics/{clinicId}/staff/crm/tasks/{receptionTaskId}/complete", null);
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var outsiderDashboard = await outsiderClient.GetAsync($"/api/clinics/{clinicId}/staff/crm-dashboard");
        outsiderDashboard.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var membership = await db.ClinicStaffMemberships.SingleAsync(member => member.ClinicId == clinicId);
            var clinic = await db.Clinics.SingleAsync(item => item.Id == clinicId);
            membership.Revoke(clinic.UserId);
            await db.SaveChangesAsync();
        }

        var revokedDashboard = await staffClient.GetAsync($"/api/clinics/{clinicId}/staff/crm-dashboard");
        revokedDashboard.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssistantCashierAndManager_ReceiveOnlyTheirOperationalTaskQueues()
    {
        var ownerEmail = $"clinic-ops-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var assistantEmail = $"clinic-ops-assistant-{Guid.NewGuid():N}@pawtrack.cr";
        var cashierEmail = $"clinic-ops-cashier-{Guid.NewGuid():N}@pawtrack.cr";
        var managerEmail = $"clinic-ops-manager-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var assistantClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, assistantEmail);
        var cashierClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, cashierEmail);
        var managerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, managerEmail);
        Guid clinicId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var assistant = await db.Users.SingleAsync(user => user.Email == assistantEmail);
            var cashier = await db.Users.SingleAsync(user => user.Email == cashierEmail);
            var manager = await db.Users.SingleAsync(user => user.Email == managerEmail);
            var clinic = Clinic.Create(owner.Id, "Clinica Operativa", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, ownerEmail);
            clinic.Activate();
            var date = new DateOnly(2026, 9, 25);
            var assistantTask = ClinicCrmTask.Create(clinic.Id, null, null, ClinicCrmTaskType.PrepareConsultation,
                date, "Preparar consultorio", null, owner.Id, ClinicInternalTaskRole.Assistant,
                ClinicCrmTaskPriority.High, assistant.Id, Guid.NewGuid());
            var cashierTask = ClinicCrmTask.Create(clinic.Id, null, null, ClinicCrmTaskType.CollectPayment,
                date, "Cobrar saldo", null, owner.Id, ClinicInternalTaskRole.Cashier,
                ClinicCrmTaskPriority.Urgent, cashier.Id, Guid.NewGuid());
            var managerTask = ClinicCrmTask.Create(clinic.Id, null, null, ClinicCrmTaskType.ReviewOperations,
                date, "Revisar operación diaria", null, owner.Id, ClinicInternalTaskRole.Manager,
                ClinicCrmTaskPriority.Normal, manager.Id, Guid.NewGuid());
            await db.Clinics.AddAsync(clinic);
            await db.ClinicStaffMemberships.AddAsync(ClinicStaffMembership.Grant(clinic.Id, assistant.Id, ClinicStaffRole.Assistant, owner.Id));
            await db.ClinicFinanceMemberships.AddRangeAsync(
                ClinicFinanceMembership.Grant(clinic.Id, cashier.Id, ClinicFinanceRole.Cashier, owner.Id),
                ClinicFinanceMembership.Grant(clinic.Id, manager.Id, ClinicFinanceRole.Administrator, owner.Id));
            await db.ClinicCrmTasks.AddRangeAsync(assistantTask, cashierTask, managerTask);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            assistantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(assistant.Id, assistant.Email, assistant.Name, assistant.Role, mfaVerified: true));
            cashierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(cashier.Id, cashier.Email, cashier.Name, cashier.Role, mfaVerified: true));
            managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(manager.Id, manager.Email, manager.Name, manager.Role, mfaVerified: true));
        }

        async Task<string[]> ReadTaskTypes(HttpClient client)
        {
            var response = await client.GetAsync($"/api/clinics/{clinicId}/staff/crm-dashboard?today=2026-09-25");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            json.RootElement.GetProperty("preferences").GetArrayLength().Should().Be(0);
            json.RootElement.GetProperty("recentActivities").GetArrayLength().Should().Be(0);
            return json.RootElement.GetProperty("openTasks").EnumerateArray()
                .Select(task => task.GetProperty("type").GetString()!)
                .ToArray();
        }

        (await ReadTaskTypes(assistantClient)).Should().Equal(nameof(ClinicCrmTaskType.PrepareConsultation));
        (await ReadTaskTypes(cashierClient)).Should().Equal(nameof(ClinicCrmTaskType.CollectPayment));
        (await ReadTaskTypes(managerClient)).Should().Equal(
            nameof(ClinicCrmTaskType.CollectPayment),
            nameof(ClinicCrmTaskType.PrepareConsultation),
            nameof(ClinicCrmTaskType.ReviewOperations));

        var deniedCashAction = await cashierClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", new
        {
            petId = (Guid?)null,
            type = nameof(ClinicCrmTaskType.CloseCash),
            dueDate = "2026-09-25",
            title = "Cerrar caja",
            idempotencyKey = Guid.NewGuid(),
            priority = nameof(ClinicCrmTaskPriority.High)
        });
        deniedCashAction.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var managerCreate = await managerClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/crm/tasks", new
        {
            petId = (Guid?)null,
            type = nameof(ClinicCrmTaskType.CloseCash),
            dueDate = "2026-09-25",
            title = "Cerrar caja",
            idempotencyKey = Guid.NewGuid(),
            priority = nameof(ClinicCrmTaskPriority.Urgent)
        });
        managerCreate.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private sealed record CrmDashboardResponse(List<CrmPreferenceResponse> Preferences);
    private sealed record CrmPreferenceResponse(Guid PetId, bool IsOptedIn);
    private sealed record OwnerClinicPreferenceResponse(Guid ClinicId, string ClinicName, string Channel, string Purpose, bool IsOptedIn);
}
