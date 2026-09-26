using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Clinics.Commands.RegisterClinic;
using PawTrack.Application.Clinics.Commands.UpdateClinicProfile;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Clinics;
using PawTrack.IntegrationTests.Infrastructure;
using PawTrack.Infrastructure.Audit;
using PawTrack.Infrastructure.Auth;
using PawTrack.Infrastructure.Clinics;
using PawTrack.Infrastructure.Migrations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Clinics;

[Collection("Integration")]
public sealed class ClinicOrganizationRegistrationEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task ClinicRegistration_CreatesSingleSiteOrganizationWithOwnerMembership()
    {
        var email = $"org-registration-{Guid.NewGuid():N}@pawtrack.cr";
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/clinics/register", new
        {
            name = "Clínica Unisede",
            licenseNumber = $"VET-{Guid.NewGuid():N}"[..12],
            address = "San José",
            lat = 9.93m,
            lng = -84.08m,
            contactEmail = email,
            password = "SecurePass1!",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var clinic = await db.Clinics.SingleAsync(site => site.ContactEmail == email);
        var siteLink = await db.ClinicOrganizationSites.SingleOrDefaultAsync(link => link.ClinicId == clinic.Id);
        siteLink.Should().NotBeNull();
        siteLink!.IsPrimary.Should().BeTrue();

        var organization = await db.ClinicOrganizations.SingleAsync(item => item.Id == siteLink.OrganizationId);
        organization.Name.Should().Be(clinic.Name);
        var ownerMembership = await db.ClinicOrganizationMemberships.SingleAsync(membership =>
            membership.OrganizationId == organization.Id && membership.UserId == clinic.UserId);
        ownerMembership.Role.Should().Be(ClinicOrganizationRole.Owner);
        ownerMembership.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task SqlServer_RegistrationAndReinvitation_PersistAndKeepHistory()
    {
        if (!OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("PAWTRACK_SQL_CONNECTION") is null)
            return;

        var databaseName = $"PawTrackClinicOrgTest_{Guid.NewGuid():N}";
        var server = Environment.GetEnvironmentVariable("PAWTRACK_SQL_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;";
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(server)
        {
            InitialCatalog = databaseName,
        };
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(connection.ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;

        await using var db = new PawTrackDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            await db.Database.ExecuteSqlRawAsync("""
                DROP INDEX [IX_ClinicOrganizationMemberships_OrganizationId_UserId]
                ON [ClinicOrganizationMemberships];
                CREATE UNIQUE INDEX [IX_ClinicOrganizationMemberships_OrganizationId_UserId]
                ON [ClinicOrganizationMemberships] ([OrganizationId], [UserId]);
                """);
            var sqlGenerator = db.GetService<IMigrationsSqlGenerator>();
            foreach (var command in sqlGenerator.Generate(
                new FilterActiveClinicOrganizationMemberships().UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);

            var email = $"owner-{Guid.NewGuid():N}@test.invalid";
            var (member, _) = User.Create($"member-{Guid.NewGuid():N}@test.invalid", "hash", "Member");
            var clinicRepository = new ClinicRepository(db);
            var register = new RegisterClinicCommandHandler(
                clinicRepository, new ClinicOrganizationRepository(db), new UserRepository(db), new PasswordHasher(), db);
            var registration = await register.Handle(new RegisterClinicCommand(
                "Unisede", $"VET-{Guid.NewGuid():N}", "San Jose", 9.93m, -84.08m, email, "SecurePass1!"),
                CancellationToken.None);
            registration.IsSuccess.Should().BeTrue();
            db.ChangeTracker.Clear();
            var clinic = await db.Clinics.AsNoTracking().SingleAsync(site => site.ContactEmail == email);
            var organization = await db.ClinicOrganizations.SingleAsync();
            var owner = await db.Users.AsNoTracking().SingleAsync(user => user.Id == clinic.UserId);
            var update = new UpdateClinicProfileCommandHandler(clinicRepository, new AuditLogRepository(db), db);
            var updated = await update.Handle(new UpdateClinicProfileCommand(
                owner.Id, "Unisede Renombrada", "Nueva direccion", null, null, null, null), CancellationToken.None);
            updated.IsSuccess.Should().BeTrue();

            db.Users.Add(member);
            await db.SaveChangesAsync();

            organization.AddMember(member.Id, ClinicOrganizationRole.Member);
            db.ClinicOrganizationMemberships.Add(organization.Memberships.Last());
            await db.SaveChangesAsync();
            organization.RevokeMember(member.Id).Should().BeTrue();
            await db.SaveChangesAsync();
            organization.AddMember(member.Id, ClinicOrganizationRole.Member);
            db.ClinicOrganizationMemberships.Add(organization.Memberships.Last());
            await db.SaveChangesAsync();

            await using var verify = new PawTrackDbContext(options);
            (await verify.Clinics.SingleAsync(site => site.Id == clinic.Id)).Name.Should().Be("Unisede Renombrada");
            (await verify.ClinicOrganizationSites.CountAsync(site => site.ClinicId == clinic.Id)).Should().Be(1);
            var memberships = await verify.ClinicOrganizationMemberships
                .Where(membership => membership.OrganizationId == organization.Id && membership.UserId == member.Id)
                .ToListAsync();
            memberships.Should().HaveCount(2);
            memberships.Should().ContainSingle(membership => membership.IsRevoked);
            memberships.Should().ContainSingle(membership => !membership.IsRevoked);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
