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
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role));
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

    private sealed record CrmDashboardResponse(List<CrmPreferenceResponse> Preferences);
    private sealed record CrmPreferenceResponse(Guid PetId, bool IsOptedIn);
    private sealed record OwnerClinicPreferenceResponse(Guid ClinicId, string ClinicName, string Channel, string Purpose, bool IsOptedIn);
}
