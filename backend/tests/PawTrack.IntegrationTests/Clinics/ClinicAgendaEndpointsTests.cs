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
