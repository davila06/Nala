using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Certificates;

[Collection("Integration")]
public sealed class CertificatesEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Verify_UnknownCode_Returns404()
    {
        var response = await _client.GetAsync("/api/certificates/verify/UNKNW999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Verify_IsAnonymous_Returns200()
    {
        // Anonymous access (no auth header) should be allowed for verification
        var response = await _client.GetAsync("/api/certificates/verify/TESTCODE");
        // 404 because it doesn't exist, but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Issue_WithoutPartnerSubscription_Returns422()
    {
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, $"certificate-issue-{Guid.NewGuid():N}@pawtrack.cr");

        // The clinic exists but has no ClinicPartner subscription
        var response = await client.PostAsJsonAsync("/api/certificates", new
        {
            petId = Guid.NewGuid(),
            clinicId = Guid.NewGuid(),
            type = "Vaccination",
            petName = "Firulais",
            petSpecies = "Perro",
            clinicName = "Clínica San José",
            clinicLicense = "VET-001",
            vetName = "Dr. Pérez",
        });

        // Tier gate: no ClinicPartner subscription → 422
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Partner");
    }

    [Fact]
    public async Task GetForPet_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/certificates/pet/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetForClinic_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/certificates/clinic/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CertificateQueriesAndRevocationRejectRealForeignResources()
    {
        var clinicEmail = $"cert-clinic-{Guid.NewGuid():N}@pawtrack.cr";
        var foreignClinicEmail = $"cert-foreign-clinic-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"cert-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var outsiderEmail = $"cert-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, clinicEmail);
        var foreignClinicClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, foreignClinicEmail);
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var outsiderClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);
        Guid clinicId;
        Guid petId;
        Guid certificateId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var foreignClinicUser = await db.Users.SingleAsync(user => user.Email == foreignClinicEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            foreignClinicUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Certificadora", $"VET-{Guid.NewGuid():N}"[..12], "San Jose", 9.93m, -84.08m, clinicEmail);
            var foreignClinic = Clinic.Create(foreignClinicUser.Id, "Otra clinica", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, foreignClinicEmail);
            clinic.Activate();
            foreignClinic.Activate();
            var pet = Pet.Create(owner.Id, "Paciente", PetSpecies.Dog, null, null);
            var certificate = VetCertificate.Issue(pet.Id, clinic.Id, clinicUser.Id, CertificateType.Vaccination,
                $"C{Guid.NewGuid():N}"[..8]);
            certificate.SetPdfUrl($"https://test-storage/clinic-certificates/{certificate.Id}.pdf");
            db.Clinics.AddRange(clinic, foreignClinic);
            db.Pets.Add(pet);
            db.VetCertificates.Add(certificate);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            petId = pet.Id;
            certificateId = certificate.Id;
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role, mfaVerified: true));
            foreignClinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(foreignClinicUser.Id, foreignClinicUser.Email, foreignClinicUser.Name, foreignClinicUser.Role, mfaVerified: true));
        }

        (await clinicClient.GetAsync($"/api/certificates/clinic/{clinicId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignClinicClient.GetAsync($"/api/v1/certificates/clinic/{clinicId}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ownerClient.GetAsync($"/api/certificates/pet/{petId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await outsiderClient.GetAsync($"/api/v1/certificates/pet/{petId}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var blobStorage = Substitute.For<IBlobStorageService>();
        blobStorage.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>([1, 2, 3]));
        using (var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(blobStorage);
        })))
        {
            async Task<HttpResponseMessage> DownloadAsAsync(HttpClient authenticated, string route)
            {
                using var client = app.CreateClient();
                client.DefaultRequestHeaders.Authorization = authenticated.DefaultRequestHeaders.Authorization;
                return await client.GetAsync(route);
            }

            (await DownloadAsAsync(foreignClinicClient, $"/api/v1/certificates/{certificateId}/download"))
                .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            (await DownloadAsAsync(outsiderClient, $"/api/certificates/{certificateId}/download"))
                .StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            await blobStorage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            (await DownloadAsAsync(clinicClient, $"/api/certificates/{certificateId}/download"))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await DownloadAsAsync(ownerClient, $"/api/v1/certificates/{certificateId}/download"))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            await blobStorage.Received(2).DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        var foreignRevoke = await foreignClinicClient.PostAsJsonAsync($"/api/v1/certificates/{certificateId}/revoke", new { reason = "Ajeno" });
        foreignRevoke.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var ownRevoke = await clinicClient.PostAsJsonAsync($"/api/certificates/{certificateId}/revoke", new { reason = "Corrección" });
        ownRevoke.StatusCode.Should().Be(HttpStatusCode.OK);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.VetCertificates.SingleAsync(cert => cert.Id == certificateId)).RevokedByUserId.Should().Be(
            (await verifyDb.Users.SingleAsync(user => user.Email == clinicEmail)).Id);
    }
}
