using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Migrations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicOrganizationModelTests
{
    [Fact]
    public void OrganizationStartsWithOwnerAndPrimarySiteAndCanAddSecondarySite()
    {
        var ownerId = Guid.CreateVersion7();
        var primaryClinicId = Guid.CreateVersion7();
        var secondaryClinicId = Guid.CreateVersion7();
        var organization = ClinicOrganization.Create("Red Veterinaria", ownerId, primaryClinicId);

        organization.AddSite(secondaryClinicId);

        organization.Sites.Should().HaveCount(2);
        organization.Sites.Should().ContainSingle(site => site.ClinicId == primaryClinicId && site.IsPrimary);
        organization.Sites.Should().ContainSingle(site => site.ClinicId == secondaryClinicId && !site.IsPrimary);
        organization.Memberships.Should().ContainSingle(membership =>
            membership.UserId == ownerId && membership.Role == ClinicOrganizationRole.Owner && !membership.IsRevoked);
    }

    [Fact]
    public void OrganizationCannotRevokeItsLastOwner()
    {
        var ownerId = Guid.CreateVersion7();
        var organization = ClinicOrganization.Create("Unisede", ownerId, Guid.CreateVersion7());

        var revoke = () => organization.RevokeMember(ownerId);

        revoke.Should().Throw<InvalidOperationException>()
            .WithMessage("An organization must retain at least one active owner.");
    }

    [Fact]
    public void ClinicModelSupportsAnOrganizationWithMultipleSitesAndOwnerMemberships()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new PawTrackDbContext(options);
        var model = db.Model;

        var organization = model.FindEntityType("PawTrack.Domain.Clinics.ClinicOrganization");
        var membership = model.FindEntityType("PawTrack.Domain.Clinics.ClinicOrganizationMembership");
        var site = model.FindEntityType("PawTrack.Domain.Clinics.ClinicOrganizationSite");

        organization.Should().NotBeNull("a unisite clinic and a multisite network share one root model");
        membership.Should().NotBeNull("owners and staff need organization-level membership identity");
        site.Should().NotBeNull("the existing Clinic ID must map to exactly one organization while preserving its identity");

        site!.GetIndexes().Should().Contain(index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(new[] { "ClinicId" }));
        membership!.GetIndexes().Should().Contain(index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { "OrganizationId", "UserId" })
                && index.GetFilter() == "[IsRevoked] = 0");
    }

    [Fact]
    public void MembershipUpgradeReplacesGlobalUniqueIndexWithActiveOnlyIndex()
    {
        var operations = new FilterActiveClinicOrganizationMemberships().UpOperations;

        operations.OfType<DropIndexOperation>().Should().ContainSingle(drop =>
            drop.Table == "ClinicOrganizationMemberships"
            && drop.Name == "IX_ClinicOrganizationMemberships_OrganizationId_UserId");
        operations.OfType<CreateIndexOperation>().Should().ContainSingle(create =>
            create.Table == "ClinicOrganizationMemberships"
            && create.IsUnique && create.Filter == "[IsRevoked] = 0");
    }
}
