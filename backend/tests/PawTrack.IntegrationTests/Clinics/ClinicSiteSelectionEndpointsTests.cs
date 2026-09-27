using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Domain.Clinics;
using PawTrack.IntegrationTests.Infrastructure;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicSiteSelectionEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task AccessibleSitesRequireExplicitSiteScopeWithinOrganization()
    {
        var ownerEmail = $"site-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var staffEmail = $"site-staff-{Guid.NewGuid():N}@pawtrack.cr";
        var legacyStaffEmail = $"site-legacy-staff-{Guid.NewGuid():N}@pawtrack.cr";
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        var legacyStaffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, legacyStaffEmail);
        Guid primaryClinicId;
        Guid secondaryClinicId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            var legacyStaff = await db.Users.SingleAsync(user => user.Email == legacyStaffEmail);
            var primary = Clinic.Create(owner.Id, "Principal", $"VET-{Guid.NewGuid():N}"[..12], "San José", 9.93m, -84.08m, ownerEmail);
            var secondary = Clinic.Create(owner.Id, "Secundaria", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, ownerEmail);
            primary.Activate();
            secondary.Activate();
            var organization = ClinicOrganization.Create("Red", owner.Id, primary.Id);
            organization.AddSite(secondary.Id);
            organization.AddMember(staff.Id, ClinicOrganizationRole.Member);
            organization.GrantSiteAccess(staff.Id, primary.Id, owner.Id);
            db.ClinicStaffMemberships.Add(ClinicStaffMembership.Grant(
                primary.Id, legacyStaff.Id, ClinicStaffRole.Assistant, owner.Id));
            db.Clinics.AddRange(primary, secondary);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSiteAccess.AddRange(organization.SiteAccess);
            await db.SaveChangesAsync();
            primaryClinicId = primary.Id;
            secondaryClinicId = secondary.Id;
        }

        var response = await staffClient.GetAsync("/api/clinics/accessible-sites");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sites = await response.Content.ReadFromJsonAsync<List<AccessibleSiteResponse>>();
        sites.Should().ContainSingle(site => site.ClinicId == primaryClinicId && site.IsPrimary);
        sites.Should().NotContain(site => site.ClinicId == secondaryClinicId);
        var activeSite = await staffClient.GetFromJsonAsync<ActiveSiteResponse>("/api/clinics/active-site");
        activeSite!.ClinicId.Should().Be(primaryClinicId);

        var legacySitesResponse = await legacyStaffClient.GetAsync("/api/clinics/accessible-sites");
        legacySitesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var legacySites = await legacySitesResponse.Content.ReadFromJsonAsync<List<AccessibleSiteResponse>>();
        legacySites.Should().ContainSingle(site => site.ClinicId == primaryClinicId);
    }

    [Fact]
    public async Task ActiveSiteSelectionIsSessionScopedAndBlocksRequestsToAnotherClinic()
    {
        var ownerEmail = $"active-site-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var secondSessionClient = await CreateSecondSessionAsync(factory, ownerEmail);
        Guid primaryClinicId;
        Guid secondaryClinicId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var primary = Clinic.Create(owner.Id, "Activa principal", $"VET-{Guid.NewGuid():N}"[..12], "San José", 9.93m, -84.08m, ownerEmail);
            var secondary = Clinic.Create(owner.Id, "Activa secundaria", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, ownerEmail);
            primary.Activate();
            secondary.Activate();
            var organization = ClinicOrganization.Create("Red activa", owner.Id, primary.Id);
            organization.AddSite(secondary.Id);
            organization.GrantSiteAccess(owner.Id, secondary.Id, owner.Id);
            db.Clinics.AddRange(primary, secondary);
            db.ClinicOrganizations.Add(organization);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationSiteAccess.AddRange(organization.SiteAccess);
            await db.SaveChangesAsync();
            primaryClinicId = primary.Id;
            secondaryClinicId = secondary.Id;
        }

        var selection = await client.PutAsJsonAsync("/api/clinics/active-site", new { clinicId = primaryClinicId });
        selection.StatusCode.Should().Be(HttpStatusCode.OK);

        var unselectedSessionAgenda = await secondSessionClient.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        unselectedSessionAgenda.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var otherSiteAgenda = await client.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        otherSiteAgenda.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var selectSecondary = await secondSessionClient.PutAsJsonAsync("/api/clinics/active-site", new { clinicId = secondaryClinicId });
        selectSecondary.StatusCode.Should().Be(HttpStatusCode.OK);
        var selectedSiteAgenda = await secondSessionClient.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        selectedSiteAgenda.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstSessionOtherSiteAgenda = await client.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        firstSessionOtherSiteAgenda.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpClient> CreateSecondSessionAsync(PawTrackWebApplicationFactory factory, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "SecurePass1!" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", document.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private sealed record AccessibleSiteResponse(Guid OrganizationId, Guid ClinicId, string ClinicName, bool IsPrimary);
    private sealed record ActiveSiteResponse(Guid? ClinicId);
}
