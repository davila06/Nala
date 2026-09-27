using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.IntegrationTests.Infrastructure;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.ServiceProviders;

[Collection("Integration")]
public sealed class ServiceProviderEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_PendingProvider_IsNotVisibleInPublicDirectory()
    {
        var uniqueName = $"Grooming {Guid.NewGuid():N}";
        var response = await _client.PostAsJsonAsync("/api/service-providers/register", new
        {
            name = uniqueName,
            description = "Cuidado profesional para mascotas",
            category = "Groomer",
            address = "Heredia centro",
            lat = 10.0m,
            lng = -84.0m,
            contactEmail = $"provider_{Guid.NewGuid():N}@pawtrack.cr",
            password = "SecurePass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var directory = await _client.GetFromJsonAsync<List<PublicProviderResponse>>(
            "/api/public/service-providers?category=Groomer");

        directory.Should().NotBeNull();
        directory!.Should().NotContain(provider => provider.Name == uniqueName);
    }

    [Fact]
    public async Task GetDirectory_WithModalityAndPrice_ReturnsVerifiedMatchingProvider()
    {
        var uniqueName = $"Verified Grooming {Guid.NewGuid():N}";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var provider = PawTrack.Domain.ServiceProviders.ServiceProvider.Create(Guid.NewGuid(), uniqueName, "Cuidado profesional", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, $"{Guid.NewGuid():N}@pawtrack.cr");
            provider.Activate();
            var service = ProviderService.Create(provider.Id, "Bano a domicilio", "Incluye traslado", ServiceModality.AtCustomerLocation, 60, 20_000m, 1);
            var verification = ProviderVerification.Submit(provider.Id, provider.UserId);
            verification.AttachDocument("https://storage.example/provider-verification/document.pdf");
            verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);
            db.ServiceProviders.Add(provider);
            db.ProviderServices.Add(service);
            db.ProviderVerifications.Add(verification);
            await db.SaveChangesAsync();
        }

        var directory = await _client.GetFromJsonAsync<List<PublicProviderResponse>>(
            "/api/public/service-providers?category=Groomer&modality=AtCustomerLocation&minPriceCrc=15000&maxPriceCrc=25000");

        directory.Should().ContainSingle(provider => provider.Name == uniqueName && provider.IsVerified);
    }

    [Fact]
    public async Task GetDirectory_InvalidModality_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/public/service-providers?modality=Unknown");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMyBookings_Anonymous_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/provider-bookings/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCalendarBookings_ReturnsOnlyOwnedPetGroomingAndTrainingBookings()
    {
        var email = $"calendar_{Guid.NewGuid():N}@pawtrack.cr";
        using var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        var startsAt = DateTimeOffset.UtcNow.AddDays(7);
        Guid petId;
        Guid foreignPetId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
            var pet = Pet.Create(userId, "Max", PetSpecies.Dog, null, null);
            var otherPet = Pet.Create(userId, "Luna", PetSpecies.Cat, null, null);
            var foreignPet = Pet.Create(Guid.NewGuid(), "Toby", PetSpecies.Dog, null, null);
            petId = pet.Id;
            foreignPetId = foreignPet.Id;
            db.Pets.AddRange(pet, otherPet, foreignPet);
            foreach (var (category, bookingPet, customerId, status) in new[]
            {
                (ServiceProviderCategory.Groomer, petId, userId, "Confirmed"),
                (ServiceProviderCategory.Trainer, petId, userId, "Confirmed"),
                (ServiceProviderCategory.Hotel, petId, userId, "Confirmed"),
                (ServiceProviderCategory.Groomer, otherPet.Id, userId, "Confirmed"),
                (ServiceProviderCategory.Groomer, petId, Guid.NewGuid(), "Confirmed"),
                (ServiceProviderCategory.Groomer, petId, userId, "Cancelled"),
                (ServiceProviderCategory.Groomer, foreignPet.Id, userId, "Confirmed"),
            })
            {
                var provider = PawTrack.Domain.ServiceProviders.ServiceProvider.Create(Guid.NewGuid(), "Proveedor", "Servicio", category,
                    "San José", 10m, -84m, $"{Guid.NewGuid():N}@pawtrack.cr");
                var service = ProviderService.Create(provider.Id, "Servicio reservado", "", ServiceModality.AtProviderLocation, 60, 1m, 1);
                var booking = ProviderBooking.Request(provider.Id, service.Id, customerId, bookingPet,
                    service.Name, startsAt, 60, 1m, 1, null);
                booking.Confirm();
                if (status == "Cancelled") booking.CancelByCustomer("Cambio de planes");
                db.ServiceProviders.Add(provider);
                db.ProviderServices.Add(service);
                db.ProviderBookings.Add(booking);
            }
            await db.SaveChangesAsync();
        }

        var from = Uri.EscapeDataString(startsAt.AddDays(-1).ToString("O"));
        var to = Uri.EscapeDataString(startsAt.AddDays(1).ToString("O"));
        var response = await client.GetAsync($"/api/provider-bookings/mine/calendar?petId={petId}&from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookings = await response.Content.ReadFromJsonAsync<List<CalendarBookingResponse>>();
        bookings.Should().HaveCount(2).And.OnlyContain(booking => booking.PetId == petId);
        bookings!.Select(booking => booking.Category).Should().BeEquivalentTo("Groomer", "Trainer");

        var foreignResponse = await client.GetAsync($"/api/provider-bookings/mine/calendar?petId={foreignPetId}&from={from}&to={to}");
        foreignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignResponse.Content.ReadFromJsonAsync<List<CalendarBookingResponse>>()).Should().BeEmpty();

        var oversizedPage = await client.GetAsync($"/api/provider-bookings/mine/calendar?petId={petId}&from={from}&to={to}&page={int.MaxValue}");
        oversizedPage.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record PublicProviderResponse(string Name, bool IsVerified);
    private sealed record CalendarBookingResponse(Guid PetId, string Category);
}
