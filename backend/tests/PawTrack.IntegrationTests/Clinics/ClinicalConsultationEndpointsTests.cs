using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicalConsultationEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task StaffVeterinarianCanDocumentAndSignOwnConsultation()
    {
        var clinicEmail = $"clinic-vet-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"vet-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"pet-vet-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid veterinarianId;
        Guid appointmentId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicUser.AssignClinicRole();
            clinicUser.ConfigureMfa("protected");
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var pet = Pet.Create(owner.Id, "Nala", PetSpecies.Dog, null, null);
            var clinic = Clinic.Create(clinicUser.Id, "Clinica Vet", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            var (grant, code) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, owner.Id, "Owner");
            grant.TryAccept(code).Should().BeTrue();
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(30));
            appointment.Confirm(); appointment.CheckIn(); appointment.StartConsultation();
            await db.Pets.AddAsync(pet);
            await db.Clinics.AddAsync(clinic);
            await db.ClinicVeterinarians.AddAsync(veterinarian);
            await db.ClinicMedicalAccessGrants.AddAsync(grant);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            veterinarianId = veterinarian.Id;
            appointmentId = appointment.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true));
        }

        var member = await clinicClient.PutAsJsonAsync("/api/clinics/me/staff/members", new
        {
            email = staffEmail,
            role = "Veterinarian",
            veterinarianId
        });
        member.StatusCode.Should().Be(HttpStatusCode.OK);
        var request = new
        {
            reason = "Control",
            subjective = "S",
            objective = "O",
            assessment = "A",
            plan = "P",
            diagnosis = "Sano",
            treatment = "Vacuna",
            ownerSummary = "Indicaciones para casa"
        };
        var createWithoutMfa = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/appointments/{appointmentId}/consultation", request);
        createWithoutMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(staff.Id, staff.Email, staff.Name, staff.Role, mfaVerified: true));
        }
        var create = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/appointments/{appointmentId}/consultation", request);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var consultation = await create.Content.ReadFromJsonAsync<ConsultationResponse>();
        var close = await staffClient.PostAsJsonAsync($"/api/clinics/{clinicId}/staff/consultations/{consultation!.Id}/close", new { signedByName = "Dra. Ana Mora" });
        close.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await close.Content.ReadFromJsonAsync<ConsultationResponse>();
        closed!.Status.Should().Be("Closed");
        using var afterScope = factory.Services.CreateScope();
        var afterDb = afterScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await afterDb.MedicalRecords.AnyAsync(record => record.PetId == closed.PetId)).Should().BeTrue();
    }

    [Fact]
    public async Task ClinicCanCreateAndCloseStructuredConsultation()
    {
        var clinicEmail = $"clinic-consult-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var ownerEmail = $"owner-consult-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);

        Guid appointmentId;
        Guid foreignConsultationId;
        string mfaToken;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicUser.AssignClinicRole();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var pet = Pet.Create(owner.Id, "Nala", PetSpecies.Dog, "Criolla", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
            await db.Pets.AddAsync(pet);
            var clinic = Clinic.Create(clinicUser.Id, "Clinica Consulta", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            await db.Clinics.AddAsync(clinic);
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            await db.ClinicVeterinarians.AddAsync(veterinarian);
            var (grant, rawCode) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, owner.Id, "Owner");
            grant.TryAccept(rawCode).Should().BeTrue();
            await db.ClinicMedicalAccessGrants.AddAsync(grant);
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(30), clinicUser.Id);
            appointment.Confirm();
            appointment.CheckIn();
            appointment.StartConsultation();
            var foreignClinic = Clinic.Create(Guid.NewGuid(), "Otra Clinica Consulta", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, $"foreign-consult-{Guid.NewGuid():N}@pawtrack.cr");
            foreignClinic.Activate();
            var foreignVeterinarian = ClinicVeterinarian.Create(foreignClinic.Id, "Dra. Otra", $"VET-{Guid.NewGuid():N}"[..12]);
            var foreignAppointment = VeterinarianAppointment.Schedule(foreignClinic.Id, foreignVeterinarian.Id, pet.Id, DateTimeOffset.UtcNow.AddHours(2), TimeSpan.FromMinutes(30));
            var foreignConsultation = ClinicalConsultation.Create(foreignClinic.Id, foreignAppointment.Id, pet.Id,
                foreignVeterinarian.Id, owner.Id, clinicUser.Id, "Control", "S", "O", "A", "P", null, null,
                null, null, null, null, null, "Sano", "Ninguno", "Resumen");
            await db.Clinics.AddAsync(foreignClinic);
            await db.ClinicVeterinarians.AddAsync(foreignVeterinarian);
            await db.VeterinarianAppointments.AddAsync(foreignAppointment);
            await db.ClinicalConsultations.AddAsync(foreignConsultation);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
            foreignConsultationId = foreignConsultation.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role));
            mfaToken = jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true);
        }

        var createPayload = new
        {
            reason = "Control general",
            subjective = "Come bien",
            objective = "Paciente alerta",
            assessment = "Estable",
            plan = "Control en seis meses",
            weightKg = 12.3m,
            temperatureC = 38.2m,
            heartRateBpm = 90,
            respiratoryRateRpm = 24,
            bodyConditionScore = 5,
            painScore = 1,
            hydrationStatus = "Normal",
            diagnosis = "Sano",
            treatment = "Vacuna anual",
            ownerSummary = "Nala está estable."
        };
        var createWithoutMfa = await clinicClient.PostAsJsonAsync($"/api/clinics/me/appointments/{appointmentId}/consultation", createPayload);
        createWithoutMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mfaToken);
        var create = await clinicClient.PostAsJsonAsync($"/api/clinics/me/appointments/{appointmentId}/consultation", createPayload);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var foreignClose = await clinicClient.PostAsJsonAsync($"/api/clinics/me/consultations/{foreignConsultationId}/close", new { signedByName = "No autorizado" });
        foreignClose.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var consultation = await create.Content.ReadFromJsonAsync<ConsultationResponse>();
        consultation.Should().NotBeNull();
        consultation!.Status.Should().Be("Draft");

        var close = await clinicClient.PostAsJsonAsync($"/api/clinics/me/consultations/{consultation.Id}/close", new { signedByName = "Dra. Ana Mora" });
        close.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await close.Content.ReadFromJsonAsync<ConsultationResponse>();
        closed.Should().NotBeNull();
        closed!.Status.Should().Be("Closed");
        closed.SignedByName.Should().Be("Dra. Ana Mora");

        using var scopeAfter = factory.Services.CreateScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var appointmentAfter = await dbAfter.VeterinarianAppointments.SingleAsync(x => x.Id == appointmentId);
        appointmentAfter.Status.Should().Be(VeterinarianAppointmentStatus.Completed);
        (await dbAfter.MedicalRecords.AnyAsync(x =>
            x.PetId == closed.PetId &&
            x.Description.Contains("Diagnóstico: Sano"))).Should().BeTrue();
    }

    private sealed record ConsultationResponse(Guid Id, Guid PetId, string Status, string? SignedByName);
}
