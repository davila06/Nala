using System.Net;
using System.Net.Http.Json;
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
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var staffClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, staffEmail);
        Guid primaryClinicId;
        Guid secondaryClinicId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var owner = await db.Users.SingleAsync(user => user.Email == ownerEmail);
            var staff = await db.Users.SingleAsync(user => user.Email == staffEmail);
            var primary = Clinic.Create(owner.Id, "Principal", $"VET-{Guid.NewGuid():N}"[..12], "San José", 9.93m, -84.08m, ownerEmail);
            var secondary = Clinic.Create(owner.Id, "Secundaria", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, ownerEmail);
            primary.Activate();
            secondary.Activate();
            var organization = ClinicOrganization.Create("Red", owner.Id, primary.Id);
            organization.AddSite(secondary.Id);
            organization.AddMember(staff.Id, ClinicOrganizationRole.Member);
            organization.GrantSiteAccess(staff.Id, primary.Id, owner.Id);
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
    }

    [Fact]
    public async Task ActiveSiteSelectionIsSessionScopedAndBlocksRequestsToAnotherClinic()
    {
        var ownerEmail = $"active-site-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
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

        var otherSiteAgenda = await client.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        otherSiteAgenda.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var selectSecondary = await client.PutAsJsonAsync("/api/clinics/active-site", new { clinicId = secondaryClinicId });
        selectSecondary.StatusCode.Should().Be(HttpStatusCode.OK);
        var selectedSiteAgenda = await client.GetAsync(
            $"/api/clinics/{secondaryClinicId}/staff/appointments?from=2026-09-26T00:00:00Z&to=2026-09-27T00:00:00Z");
        selectedSiteAgenda.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record AccessibleSiteResponse(Guid OrganizationId, Guid ClinicId, string ClinicName, bool IsPrimary);
}
