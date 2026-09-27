using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.IntegrationTests.Infrastructure;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicOrganizationSiteAccessEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task OwnerAndAdminManageSiteGrantsWithMfaMembershipScopeAndAudit()
    {
        var ownerEmail = $"site-grant-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var adminEmail = $"site-grant-admin-{Guid.NewGuid():N}@pawtrack.cr";
        var memberEmail = $"site-grant-member-{Guid.NewGuid():N}@pawtrack.cr";
        var otherMemberEmail = $"site-grant-other-{Guid.NewGuid():N}@pawtrack.cr";
        var outsiderEmail = $"site-grant-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var ownerClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, ownerEmail);
        var adminClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, adminEmail);
        var memberClient = await AuthHelper.CreateAuthenticatedClientAsync(factory, memberEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, otherMemberEmail);
        _ = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);

        Guid primaryClinicId;
        Guid secondaryClinicId;
        Guid foreignClinicId;
        Guid adminId;
        Guid memberId;
        Guid otherMemberId;
        Guid outsiderId;
        Guid ownerId;
        Guid adminSessionId;
        Guid ownerSessionId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var users = await db.Users.Where(user => user.Email == ownerEmail
                    || user.Email == adminEmail
                    || user.Email == memberEmail
                    || user.Email == otherMemberEmail
                    || user.Email == outsiderEmail)
                .ToDictionaryAsync(user => user.Email);
            var owner = users[ownerEmail];
            var admin = users[adminEmail];
            var member = users[memberEmail];
            var otherMember = users[otherMemberEmail];
            var outsider = users[outsiderEmail];
            owner.ConfigureMfa("owner-mfa-test");
            admin.ConfigureMfa("admin-mfa-test");

            var primary = Clinic.Create(owner.Id, "Org principal", $"VET-{Guid.NewGuid():N}"[..12], "San José", 9.93m, -84.08m, ownerEmail);
            var secondary = Clinic.Create(owner.Id, "Org secundaria", $"VET-{Guid.NewGuid():N}"[..12], "Cartago", 9.86m, -83.92m, ownerEmail);
            var foreign = Clinic.Create(outsider.Id, "Otra organización", $"VET-{Guid.NewGuid():N}"[..12], "Heredia", 9.99m, -84.12m, outsiderEmail);
            primary.Activate();
            secondary.Activate();
            foreign.Activate();

            var organization = ClinicOrganization.Create("Red de prueba", owner.Id, primary.Id);
            organization.AddSite(secondary.Id);
            organization.AddMember(admin.Id, ClinicOrganizationRole.Administrator);
            organization.AddMember(member.Id, ClinicOrganizationRole.Member);
            organization.AddMember(otherMember.Id, ClinicOrganizationRole.Member);
            var foreignOrganization = ClinicOrganization.Create("Red externa", outsider.Id, foreign.Id);

            db.Clinics.AddRange(primary, secondary, foreign);
            db.ClinicOrganizations.AddRange(organization, foreignOrganization);
            db.ClinicOrganizationSites.AddRange(organization.Sites);
            db.ClinicOrganizationSites.AddRange(foreignOrganization.Sites);
            db.ClinicOrganizationMemberships.AddRange(organization.Memberships);
            db.ClinicOrganizationMemberships.AddRange(foreignOrganization.Memberships);
            db.ClinicOrganizationSiteAccess.AddRange(organization.SiteAccess);
            db.ClinicOrganizationSiteAccess.AddRange(foreignOrganization.SiteAccess);
            await db.SaveChangesAsync();

            var jwt = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.IJwtTokenService>();
            adminSessionId = await db.RefreshTokens.Where(token => token.UserId == admin.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            ownerSessionId = await db.RefreshTokens.Where(token => token.UserId == owner.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt).Select(token => token.SessionId).FirstAsync();
            adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(admin.Id, admin.Email, admin.Name, admin.Role, mfaVerified: true, sessionId: adminSessionId));
            ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(owner.Id, owner.Email, owner.Name, owner.Role, mfaVerified: true, sessionId: ownerSessionId));
            memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(member.Id, member.Email, member.Name, member.Role, mfaVerified: true));

            primaryClinicId = primary.Id;
            secondaryClinicId = secondary.Id;
            foreignClinicId = foreign.Id;
            adminId = admin.Id;
            memberId = member.Id;
            otherMemberId = otherMember.Id;
            outsiderId = outsider.Id;
            ownerId = owner.Id;
        }

        using var noMfaAdmin = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var admin = await db.Users.SingleAsync(user => user.Id == adminId);
            var jwt = scope.ServiceProvider.GetRequiredService<PawTrack.Application.Common.Interfaces.IJwtTokenService>();
            noMfaAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                jwt.GenerateAccessToken(admin.Id, admin.Email, admin.Name, admin.Role, sessionId: adminSessionId));
        }

        var grantWithoutMfa = await noMfaAdmin.PutAsync(
            $"/api/clinics/{secondaryClinicId}/site-access/{memberId}", null);
        grantWithoutMfa.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var memberGrant = await memberClient.PutAsync(
            $"/api/clinics/{secondaryClinicId}/site-access/{otherMemberId}", null);
        memberGrant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var memberList = await memberClient.GetAsync($"/api/clinics/{secondaryClinicId}/site-access");
        memberList.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var outsiderGrant = await adminClient.PutAsync(
            $"/api/clinics/{secondaryClinicId}/site-access/{outsiderId}", null);
        outsiderGrant.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var grant = await adminClient.PutAsync($"/api/clinics/{secondaryClinicId}/site-access/{memberId}", null);
        grant.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await adminClient.GetFromJsonAsync<List<SiteAccessResponse>>(
            $"/api/clinics/{secondaryClinicId}/site-access");
        list.Should().ContainSingle(access => access.UserId == memberId);

        var memberSites = await memberClient.GetFromJsonAsync<List<AccessibleSiteResponse>>("/api/clinics/accessible-sites");
        memberSites.Should().ContainSingle(site => site.ClinicId == secondaryClinicId);

        var foreignOrganizationGrant = await adminClient.PutAsync(
            $"/api/clinics/{foreignClinicId}/site-access/{memberId}", null);
        foreignOrganizationGrant.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var revoke = await adminClient.DeleteAsync($"/api/clinics/{secondaryClinicId}/site-access/{memberId}");
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var sitesAfterRevoke = await memberClient.GetFromJsonAsync<List<AccessibleSiteResponse>>("/api/clinics/accessible-sites");
        sitesAfterRevoke.Should().NotContain(site => site.ClinicId == secondaryClinicId);

        var ownerGrant = await ownerClient.PutAsync($"/api/clinics/{secondaryClinicId}/site-access/{otherMemberId}", null);
        ownerGrant.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var ownerRevoke = await ownerClient.DeleteAsync($"/api/clinics/{secondaryClinicId}/site-access/{otherMemberId}");
        ownerRevoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var revokeLastOwnerSite = await ownerClient.DeleteAsync($"/api/clinics/{primaryClinicId}/site-access/{ownerId}");
        revokeLastOwnerSite.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        (await verifyDb.AuditLog.CountAsync(entry => entry.AdminUserId == adminId
            && entry.EntityType == "ClinicOrganizationSiteAccess"
            && entry.Action == AuditAction.ClinicSiteAccessGranted)).Should().Be(1);
        (await verifyDb.AuditLog.CountAsync(entry => entry.AdminUserId == adminId
            && entry.EntityType == "ClinicOrganizationSiteAccess"
            && entry.Action == AuditAction.ClinicSiteAccessRevoked)).Should().Be(1);
        (await verifyDb.AuditLog.CountAsync(entry => entry.AdminUserId == ownerId
            && entry.EntityType == "ClinicOrganizationSiteAccess"
            && entry.Action == AuditAction.ClinicSiteAccessGranted)).Should().Be(1);
        (await verifyDb.AuditLog.CountAsync(entry => entry.AdminUserId == ownerId
            && entry.EntityType == "ClinicOrganizationSiteAccess"
            && entry.Action == AuditAction.ClinicSiteAccessRevoked)).Should().Be(1);
    }

    private sealed record SiteAccessResponse(Guid UserId, Guid ClinicId, Guid GrantedByUserId, DateTimeOffset GrantedAt);
    private sealed record AccessibleSiteResponse(Guid OrganizationId, string OrganizationName, Guid ClinicId, string ClinicName, bool IsPrimary);
}
