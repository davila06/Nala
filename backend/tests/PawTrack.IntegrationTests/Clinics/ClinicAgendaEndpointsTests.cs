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
public sealed class ClinicAgendaEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task ReceptionistCanConfirmOnlyItsClinicAppointment()
    {
        var clinicEmail = $"clinic-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"reception-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        Guid clinicId;
        Guid foreignClinicId;
        Guid appointmentId;
        var startsAt = DateTimeOffset.UtcNow.AddDays(1);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            owner.AssignClinicRole();
            owner.ConfigureMfa("protected");
            var clinic = Clinic.Create(owner.Id, "Clinica Staff", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var foreignClinic = Clinic.Create(Guid.NewGuid(), "Clinica Ajena", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, "foreign@test.cr");
            foreignClinic.Activate();
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, Guid.NewGuid(), Guid.NewGuid(), startsAt, TimeSpan.FromMinutes(30));
            await db.Clinics.AddRangeAsync(clinic, foreignClinic);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            foreignClinicId = foreignClinic.Id;
            appointmentId = appointment.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(owner.Id, owner.Email, owner.Name, owner.Role, mfaVerified: true));
        }

        var grant = await clinicClient.PutAsJsonAsync("/api/clinics/me/staff/members", new { email = staffEmail, role = "Receptionist" });
        grant.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspaces = await staffClient.GetAsync("/api/clinics/staff-workspaces");
        workspaces.StatusCode.Should().Be(HttpStatusCode.OK);
        var own = await staffClient.GetAsync($"/api/clinics/{clinicId}/staff/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        var foreign = await staffClient.GetAsync($"/api/clinics/{foreignClinicId}/staff/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        foreign.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var stepUpRequired = await staffClient.PatchAsJsonAsync($"/api/clinics/{clinicId}/staff/appointments/{appointmentId}/status", new { status = "Confirmed" });
        stepUpRequired.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(staff.Id, staff.Email, staff.Name, staff.Role, mfaVerified: true));
        }
        var foreignMutation = await staffClient.PatchAsJsonAsync($"/api/clinics/{foreignClinicId}/staff/appointments/{appointmentId}/status", new { status = "Confirmed" });
        foreignMutation.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var confirm = await staffClient.PatchAsJsonAsync($"/api/clinics/{clinicId}/staff/appointments/{appointmentId}/status", new { status = "Confirmed" });
        confirm.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task FinanceMemberCanReadItsClinicAgendaButNotAnotherClinic()
    {
        var ownerEmail = $"clinic-finance-agenda-{Guid.NewGuid():N}@pawtrack.cr";
        var cashierEmail = $"cashier-agenda-{Guid.NewGuid():N}@pawtrack.cr";
        var cashierClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, cashierEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid foreignClinicId;
        DateTimeOffset startsAt;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var cashier = await db.Users.SingleAsync(user => user.Email == cashierEmail);
            var clinic = Clinic.Create(owner.Id, "Clinica Finanzas Agenda", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, ownerEmail);
            clinic.Activate();
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            var pet = Pet.Create(owner.Id, "Max", PetSpecies.Dog, null, null);
            startsAt = DateTimeOffset.UtcNow.AddDays(1);
            var appointment = VeterinarianAppointment.Schedule(clinic.Id, veterinarian.Id, pet.Id, startsAt, TimeSpan.FromMinutes(30));
            var foreignClinic = Clinic.Create(Guid.NewGuid(), "Clinica Ajena", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, $"foreign-{Guid.NewGuid():N}@pawtrack.cr");
            foreignClinic.Activate();
            await db.Clinics.AddRangeAsync(clinic, foreignClinic);
            await db.ClinicVeterinarians.AddAsync(veterinarian);
            await db.Pets.AddAsync(pet);
            await db.VeterinarianAppointments.AddAsync(appointment);
            await db.ClinicFinanceMemberships.AddAsync(ClinicFinanceMembership.Grant(clinic.Id, cashier.Id, ClinicFinanceRole.Cashier, owner.Id));
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            foreignClinicId = foreignClinic.Id;
            cashierClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(cashier.Id, cashier.Email, cashier.Name, cashier.Role));
        }

        var own = await cashierClient.GetAsync($"/api/clinics/{clinicId}/staff/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        var appointments = await own.Content.ReadFromJsonAsync<List<AgendaItemResponse>>();
        appointments.Should().ContainSingle();

        var foreign = await cashierClient.GetAsync($"/api/clinics/{foreignClinicId}/staff/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        foreign.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ClinicCanScheduleListAndConfirmAppointment()
    {
        var clinicEmail = $"clinic-agenda-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, clinicEmail);
        var ownerEmail = $"owner-agenda-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);

        Guid clinicId;
        Guid veterinarianId;
        Guid petId;
        var startsAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(2);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicUser.AssignClinicRole();

            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var pet = Pet.Create(owner.Id, "Nala", PetSpecies.Dog, "Criolla", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-3)));
            await db.Pets.AddAsync(pet);

            var clinic = Clinic.Create(
                clinicUser.Id,
                "Clinica Agenda",
                $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose",
                9.93m,
                -84.08m,
                clinicEmail);
            clinic.Activate();
            await db.Clinics.AddAsync(clinic);

            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana Mora", $"VET-{Guid.NewGuid():N}"[..12]);
            await db.ClinicVeterinarians.AddAsync(veterinarian);

            await db.SaveChangesAsync();

            clinicId = clinic.Id;
            veterinarianId = veterinarian.Id;
            petId = pet.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role));
        }

        var createWithoutMfa = await clinicClient.PostAsJsonAsync(
            $"/api/clinics/me/veterinarians/{veterinarianId}/appointments",
            new { petId, startsAt, durationMinutes = 30 });
        createWithoutMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true));
        }
        var create = await clinicClient.PostAsJsonAsync(
            $"/api/clinics/me/veterinarians/{veterinarianId}/appointments",
            new { petId, startsAt, durationMinutes = 30 });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreateAppointmentResponse>();
        created.Should().NotBeNull();

        var agenda = await clinicClient.GetAsync(
            $"/api/clinics/me/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        agenda.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await agenda.Content.ReadFromJsonAsync<List<AgendaItemResponse>>();
        items.Should().ContainSingle(item =>
            item.AppointmentId == created!.AppointmentId &&
            item.ClinicId == clinicId &&
            item.PetName == "Nala" &&
            item.VeterinarianName == "Dra. Ana Mora" &&
            item.Status == "Scheduled");

        var update = await clinicClient.PatchAsJsonAsync(
            $"/api/clinics/me/appointments/{created!.AppointmentId}/status",
            new { status = "Confirmed" });
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var agendaAfter = await clinicClient.GetAsync(
            $"/api/clinics/me/appointments?from={Uri.EscapeDataString(startsAt.AddHours(-1).ToString("O"))}&to={Uri.EscapeDataString(startsAt.AddHours(2).ToString("O"))}");
        var afterItems = await agendaAfter.Content.ReadFromJsonAsync<List<AgendaItemResponse>>();
        afterItems.Should().ContainSingle(item =>
            item.AppointmentId == created.AppointmentId && item.Status == "Confirmed");
    }

    private sealed record CreateAppointmentResponse(Guid AppointmentId);

    private sealed record AgendaItemResponse(
        Guid AppointmentId,
        Guid ClinicId,
        Guid VeterinarianId,
        string VeterinarianName,
        Guid PetId,
        string PetName,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        string Status);
}
