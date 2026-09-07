using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.IntegrationTests.Infrastructure;
using PawTrack.Domain.ServiceProviders;
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

    private sealed record PublicProviderResponse(string Name, bool IsVerified);
}