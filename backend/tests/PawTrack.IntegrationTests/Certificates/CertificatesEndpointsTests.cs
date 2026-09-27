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
using PawTrack.Domain.Medical;
using PawTrack.Domain.Subscriptions;
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
        var clinicEmail = $"certificate-issue-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"certificate-issue-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, clinicEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid petId;
        Guid veterinarianId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Clínica sin Partner", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, clinicEmail);
            clinic.Activate();
            var pet = Pet.Create(owner.Id, "Firulais", PetSpecies.Dog, null, null);
            var (grant, code) = ClinicMedicalAccessGrant.Generate(pet.Id, clinic.Id, owner.Id, "Owner");
            grant.TryAccept(code).Should().BeTrue();
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dr. Pérez", "VET-DR-001");
            var organization = ClinicOrganization.Create("Organización de prueba", clinicUser.Id, clinic.Id);
            db.Clinics.Add(clinic);
            db.Pets.Add(pet);
            db.ClinicMedicalAccessGrants.Add(grant);
            db.ClinicVeterinarians.Add(veterinarian);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            petId = pet.Id;
            veterinarianId = veterinarian.Id;
        }

        (await client.PutAsJsonAsync("/api/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await client.PostAsJsonAsync("/api/certificates", new
        {
            petId,
            clinicId,
            type = "Vaccination",
            petName = "Firulais",
            petSpecies = "Perro",
            clinicName = "Clínica San José",
            clinicLicense = "VET-001",
            veterinarianId,
            vetName = "Dr. Pérez",
        });

        // Tier gate: no ClinicPartner subscription → 422
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Partner");
    }

    [Fact]
    public async Task PartnerCertificateIssueRequiresOwnClinicAndActivePetGrant()
    {
        var clinicEmail = $"issue-clinic-{Guid.NewGuid():N}@pawtrack.cr";
        var foreignEmail = $"issue-foreign-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"issue-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, clinicEmail);
        var foreignClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, foreignEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid veterinarianId;
        Guid authorizedPetId;
        Guid unauthorizedPetId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var foreignUser = await db.Users.SingleAsync(user => user.Email == foreignEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            foreignUser.AssignClinicRole();
            var clinic = Clinic.Create(clinicUser.Id, "Partner emisor", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, clinicEmail);
            var foreignClinic = Clinic.Create(foreignUser.Id, "Partner ajeno", $"VET-{Guid.NewGuid():N}"[..12],
                "Cartago", 9.86m, -83.92m, foreignEmail);
            clinic.Activate();
            foreignClinic.Activate();
            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Registrada", "VET-DR-100");
            var authorizedPet = Pet.Create(owner.Id, "Paciente autorizado", PetSpecies.Dog, null, null);
            var unauthorizedPet = Pet.Create(owner.Id, "Sin permiso", PetSpecies.Dog, null, null);
            var (grant, code) = ClinicMedicalAccessGrant.Generate(authorizedPet.Id, clinic.Id, owner.Id, "Owner");
            grant.TryAccept(code).Should().BeTrue();
            var plan = Subscription.CreateForClinic(clinic.Id, clinicUser.Id, SubscriptionTier.ClinicPartner,
                $"I{Guid.NewGuid():N}"[..8], 35000m);
            plan.Activate();
            var organization = ClinicOrganization.Create("Red emisora", clinicUser.Id, clinic.Id);
            organization.AddSite(foreignClinic.Id);
            organization.AddMember(foreignUser.Id, ClinicOrganizationRole.Member);
            organization.GrantSiteAccess(foreignUser.Id, clinic.Id, clinicUser.Id);
            db.Clinics.AddRange(clinic, foreignClinic);
            db.ClinicVeterinarians.Add(veterinarian);
            db.Pets.AddRange(authorizedPet, unauthorizedPet);
            db.ClinicMedicalAccessGrants.Add(grant);
            db.Subscriptions.Add(plan);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicOrganizationSiteAccess.AddRange(organization.SiteAccess);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            veterinarianId = veterinarian.Id;
            authorizedPetId = authorizedPet.Id;
            unauthorizedPetId = unauthorizedPet.Id;
            var clinicSessionId = await db.RefreshTokens.Where(token => token.UserId == clinicUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            var foreignSessionId = await db.RefreshTokens.Where(token => token.UserId == foreignUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role,
                    mfaVerified: true, sessionId: clinicSessionId));
            foreignClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(foreignUser.Id, foreignUser.Email, foreignUser.Name, foreignUser.Role,
                    mfaVerified: true, sessionId: foreignSessionId));
        }

        (await clinicClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignClient.PutAsJsonAsync("/api/v1/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);

        object IssuePayload(Guid petId, Guid? selectedVeterinarianId = null) => new
        {
            veterinarianId = selectedVeterinarianId ?? veterinarianId,
            petId,
            clinicId,
            type = "Vaccination",
            petName = "Paciente autorizado",
            petSpecies = "Dog",
            clinicName = "Partner emisor",
            clinicLicense = "VET-001",
            vetName = "Dra. Mora",
        };
        var otherClinic = await foreignClient.PostAsJsonAsync("/api/v1/certificates", IssuePayload(authorizedPetId));
        otherClinic.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var noGrant = await clinicClient.PostAsJsonAsync("/api/certificates", IssuePayload(unauthorizedPetId));
        noGrant.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var unregisteredVeterinarian = await clinicClient.PostAsJsonAsync("/api/certificates",
            IssuePayload(authorizedPetId, Guid.NewGuid()));
        unregisteredVeterinarian.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var own = await clinicClient.PostAsJsonAsync("/api/certificates", IssuePayload(authorizedPetId));
        own.StatusCode.Should().Be(HttpStatusCode.Created);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.VetCertificates.CountAsync(cert => cert.ClinicId == clinicId)).Should().Be(1);
        (await verifyDb.VetCertificates.SingleAsync(cert => cert.ClinicId == clinicId)).PetId.Should().Be(authorizedPetId);
    }

    [Fact]
    public async Task VaccinePassportRequiresOwnVeterinarianAndActiveGrantAndUsesPersistedIdentity()
    {
        var clinicEmail = $"passport-clinic-{Guid.NewGuid():N}@pawtrack.cr";
        var foreignEmail = $"passport-foreign-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerEmail = $"passport-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var clinicClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, clinicEmail);
        var foreignClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, foreignEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        Guid clinicId;
        Guid foreignClinicId;
        Guid authorizedPetId;
        Guid ungrantedPetId;
        Guid veterinarianId;
        Guid foreignVeterinarianId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var clinicUser = await db.Users.SingleAsync(user => user.Email == clinicEmail);
            var foreignUser = await db.Users.SingleAsync(user => user.Email == foreignEmail);
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            clinicUser.AssignClinicRole();
            foreignUser.AssignClinicRole();

            var clinic = Clinic.Create(clinicUser.Id, "Pasaportes CR", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, clinicEmail);
            var foreignClinic = Clinic.Create(foreignUser.Id, "Pasaportes ajenos", $"VET-{Guid.NewGuid():N}"[..12],
                "Cartago", 9.86m, -83.92m, foreignEmail);
            clinic.Activate();
            foreignClinic.Activate();

            var authorizedPet = Pet.Create(owner.Id, "Luna persistida", PetSpecies.Dog, "Criolla", null);
            var ungrantedPet = Pet.Create(owner.Id, "Sin grant", PetSpecies.Dog, "Mestiza", null);
            var (grant, code) = ClinicMedicalAccessGrant.Generate(authorizedPet.Id, clinic.Id, owner.Id, "Owner");
            grant.TryAccept(code).Should().BeTrue();

            var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Persistida", "VET-CR-001");
            var foreignVeterinarian = ClinicVeterinarian.Create(foreignClinic.Id, "Dr. Ajeno", "VET-CR-002");
            var verification = ClinicVerification.Submit(clinic.Id, clinic.LicenseNumber);
            verification.AttachDocument("https://test-storage/verification.pdf");
            verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))).IsSuccess.Should().BeTrue();
            var plan = Subscription.CreateForClinic(clinic.Id, clinicUser.Id, SubscriptionTier.ClinicPartner,
                $"P{Guid.NewGuid():N}"[..8], 35000m);
            plan.Activate();
            var organization = ClinicOrganization.Create("Red pasaportes", clinicUser.Id, clinic.Id);

            db.Clinics.AddRange(clinic, foreignClinic);
            db.Pets.AddRange(authorizedPet, ungrantedPet);
            db.ClinicMedicalAccessGrants.Add(grant);
            db.ClinicVeterinarians.AddRange(veterinarian, foreignVeterinarian);
            db.ClinicVerifications.Add(verification);
            db.Subscriptions.Add(plan);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            await db.SaveChangesAsync();

            clinicId = clinic.Id;
            foreignClinicId = foreignClinic.Id;
            authorizedPetId = authorizedPet.Id;
            ungrantedPetId = ungrantedPet.Id;
            veterinarianId = veterinarian.Id;
            foreignVeterinarianId = foreignVeterinarian.Id;
            var clinicSessionId = await db.RefreshTokens.Where(token => token.UserId == clinicUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            var foreignSessionId = await db.RefreshTokens.Where(token => token.UserId == foreignUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role,
                    mfaVerified: true, sessionId: clinicSessionId));
            foreignClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(foreignUser.Id, foreignUser.Email, foreignUser.Name, foreignUser.Role,
                    mfaVerified: true, sessionId: foreignSessionId));
        }

        (await clinicClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignClient.PutAsJsonAsync("/api/v1/clinics/active-site", new { clinicId = foreignClinicId })).StatusCode.Should().Be(HttpStatusCode.OK);

        object PassportPayload(Guid petId, Guid selectedVeterinarianId) => new
        {
            petId,
            clinicId,
            veterinarianId = selectedVeterinarianId,
            vetName = "Veterinario falso",
            vetLicense = "VET-FALSO",
            petColor = "Color falso",
            vaccines = new[] { new { vaccineName = "Rabia", brand = "Marca", applicationDate = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd") } },
        };

        var foreignVeterinarianResponse = await clinicClient.PostAsJsonAsync("/api/certificates/passport",
            PassportPayload(authorizedPetId, foreignVeterinarianId));
        foreignVeterinarianResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var noGrant = await clinicClient.PostAsJsonAsync("/api/v1/certificates/passport",
            PassportPayload(ungrantedPetId, veterinarianId));
        noGrant.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var own = await clinicClient.PostAsJsonAsync("/api/certificates/passport",
            PassportPayload(authorizedPetId, veterinarianId));
        own.StatusCode.Should().Be(HttpStatusCode.Created);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var certificate = await verifyDb.VetCertificates.SingleAsync(item => item.ClinicId == clinicId && item.PetId == authorizedPetId);
        var passport = await verifyDb.VaccinePassports.SingleAsync(item => item.CertificateId == certificate.Id);
        passport.VetNameSnapshot.Should().Be("Dra. Persistida");
        passport.VetLicenseSnapshot.Should().Be("VET-CR-001");
        passport.PetNameSnapshot.Should().Be("Luna persistida");
        passport.PetColorSnapshot.Should().NotBe("Color falso");
    }

    [Fact]
    public async Task VeterinarianPermissionsRejectForeignNestedResourceWithoutMutation()
    {
        var firstEmail = $"vet-permissions-a-{Guid.NewGuid():N}@pawtrack.cr";
        var secondEmail = $"vet-permissions-b-{Guid.NewGuid():N}@pawtrack.cr";
        var firstClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, firstEmail);
        var secondClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, secondEmail);
        Guid firstClinicId;
        Guid secondClinicId;
        Guid firstVeterinarianId;
        Guid secondVeterinarianId;
        string foreignPermissions;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var firstUser = await db.Users.SingleAsync(user => user.Email == firstEmail);
            var secondUser = await db.Users.SingleAsync(user => user.Email == secondEmail);
            firstUser.AssignClinicRole();
            secondUser.AssignClinicRole();
            var firstClinic = Clinic.Create(firstUser.Id, "Clinica permisos A", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, firstEmail);
            var secondClinic = Clinic.Create(secondUser.Id, "Clinica permisos B", $"VET-{Guid.NewGuid():N}"[..12],
                "Cartago", 9.86m, -83.92m, secondEmail);
            firstClinic.Activate();
            secondClinic.Activate();
            var firstVeterinarian = ClinicVeterinarian.Create(firstClinic.Id, "Dra. Permisos A", "VET-A-001");
            var secondVeterinarian = ClinicVeterinarian.Create(secondClinic.Id, "Dr. Permisos B", "VET-B-001");
            var firstOrganization = ClinicOrganization.Create("Organizacion A", firstUser.Id, firstClinic.Id);
            var secondOrganization = ClinicOrganization.Create("Organizacion B", secondUser.Id, secondClinic.Id);
            db.Clinics.AddRange(firstClinic, secondClinic);
            db.ClinicVeterinarians.AddRange(firstVeterinarian, secondVeterinarian);
            db.ClinicOrganizations.AddRange(firstOrganization, secondOrganization);
            db.ClinicOrganizationMemberships.AddRange(firstOrganization.Memberships);
            db.ClinicOrganizationSites.AddRange(firstOrganization.Sites);
            db.ClinicOrganizationMemberships.AddRange(secondOrganization.Memberships);
            db.ClinicOrganizationSites.AddRange(secondOrganization.Sites);
            await db.SaveChangesAsync();
            firstClinicId = firstClinic.Id;
            secondClinicId = secondClinic.Id;
            firstVeterinarianId = firstVeterinarian.Id;
            secondVeterinarianId = secondVeterinarian.Id;
            foreignPermissions = secondVeterinarian.Permissions;
            var firstSessionId = await db.RefreshTokens.Where(token => token.UserId == firstUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            var secondSessionId = await db.RefreshTokens.Where(token => token.UserId == secondUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            firstClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(firstUser.Id, firstUser.Email, firstUser.Name, firstUser.Role,
                    mfaVerified: true, sessionId: firstSessionId));
            secondClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(secondUser.Id, secondUser.Email, secondUser.Name, secondUser.Role,
                    mfaVerified: true, sessionId: secondSessionId));
        }

        (await firstClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId = firstClinicId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await secondClient.PutAsJsonAsync("/api/v1/clinics/active-site", new { clinicId = secondClinicId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var own = await firstClient.PutAsJsonAsync($"/api/clinics/me/veterinarians/{firstVeterinarianId}/permissions",
            new { permissions = new[] { ClinicVeterinarianPermission.ViewMedical } });
        own.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var foreign = await firstClient.PutAsJsonAsync($"/api/v1/clinics/me/veterinarians/{secondVeterinarianId}/permissions",
            new { permissions = new[] { ClinicVeterinarianPermission.ExportMedical } });
        foreign.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicVeterinarians.SingleAsync(item => item.Id == secondVeterinarianId)).Permissions
            .Should().Be(foreignPermissions);
    }

    [Fact]
    public async Task AdminVeterinarianReviewRequiresAdminRoleAndFreshMfa()
    {
        var ownerEmail = $"vet-review-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var adminEmail = $"vet-review-admin-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerClient = await AuthHelper.CreateMfaAuthenticatedClientAsync(factory, ownerEmail);
        var adminClient = await AuthHelper.CreateAdminClientAsync(factory, adminEmail);
        Guid veterinarianId;
        Guid clinicId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            owner.AssignClinicRole();
            var clinic = Clinic.Create(owner.Id, "Clinica revisión veterinaria", $"VET-{Guid.NewGuid():N}"[..12],
                "San Jose", 9.93m, -84.08m, ownerEmail);
            var veterinarian = ClinicVeterinarian.Submit(clinic.Id, owner.Id, "Dra. Pendiente", "VET-PENDING-001");
            veterinarian.AttachDocument("https://test-storage/vet-document.pdf");
            db.Clinics.Add(clinic);
            db.ClinicVeterinarians.Add(veterinarian);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            veterinarianId = veterinarian.Id;
        }

        (await ownerClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownerClient.PutAsJsonAsync($"/api/clinics/admin/veterinarians/{veterinarianId}/review",
            new { approve = true, expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await adminClient.PutAsJsonAsync($"/api/v1/clinics/admin/veterinarians/{veterinarianId}/review",
            new { approve = true, expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var admin = await db.Users.SingleAsync(user => user.Email == adminEmail);
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(admin.Id, admin.Email, admin.Name, admin.Role, mfaVerified: true));
        }

        (await adminClient.PutAsJsonAsync($"/api/clinics/admin/veterinarians/{veterinarianId}/review",
            new { approve = true, expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)).ToString("yyyy-MM-dd") }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        (await verifyDb.ClinicVeterinarians.SingleAsync(item => item.Id == veterinarianId)).Status
            .Should().Be(ClinicVeterinarianStatus.Authorized);
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
        Guid foreignClinicId;
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
            var clinicOrganization = ClinicOrganization.Create("Red certificadora", clinicUser.Id, clinic.Id);
            var foreignOrganization = ClinicOrganization.Create("Red externa", foreignClinicUser.Id, foreignClinic.Id);
            var pet = Pet.Create(owner.Id, "Paciente", PetSpecies.Dog, null, null);
            var certificate = VetCertificate.Issue(pet.Id, clinic.Id, clinicUser.Id, CertificateType.Vaccination,
                $"C{Guid.NewGuid():N}"[..8]);
            certificate.SetPdfUrl($"https://test-storage/clinic-certificates/{certificate.Id}.pdf");
            db.Clinics.AddRange(clinic, foreignClinic);
            db.ClinicOrganizations.AddRange(clinicOrganization, foreignOrganization);
            db.ClinicOrganizationMemberships.AddRange(clinicOrganization.Memberships);
            db.ClinicOrganizationSites.AddRange(clinicOrganization.Sites);
            db.ClinicOrganizationMemberships.AddRange(foreignOrganization.Memberships);
            db.ClinicOrganizationSites.AddRange(foreignOrganization.Sites);
            db.Pets.Add(pet);
            db.VetCertificates.Add(certificate);
            await db.SaveChangesAsync();
            clinicId = clinic.Id;
            foreignClinicId = foreignClinic.Id;
            petId = pet.Id;
            certificateId = certificate.Id;
            var clinicSessionId = await db.RefreshTokens.Where(token => token.UserId == clinicUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            var foreignSessionId = await db.RefreshTokens.Where(token => token.UserId == foreignClinicUser.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            clinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(clinicUser.Id, clinicUser.Email, clinicUser.Name, clinicUser.Role,
                    mfaVerified: true, sessionId: clinicSessionId));
            foreignClinicClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(foreignClinicUser.Id, foreignClinicUser.Email, foreignClinicUser.Name, foreignClinicUser.Role,
                    mfaVerified: true, sessionId: foreignSessionId));
        }

        (await clinicClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignClinicClient.PutAsJsonAsync("/api/v1/clinics/active-site", new { clinicId = foreignClinicId }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await clinicClient.GetAsync($"/api/certificates/clinic/{clinicId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await foreignClinicClient.GetAsync($"/api/v1/certificates/clinic/{clinicId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
