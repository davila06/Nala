using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PawTrack.Infrastructure.Persistence;

#nullable disable

namespace PawTrack.Infrastructure.Migrations;

[DbContext(typeof(PawTrackDbContext))]
[Migration("20260926183000_FilterActiveClinicOrganizationMemberships")]
public sealed class FilterActiveClinicOrganizationMemberships : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ClinicOrganizationMemberships_OrganizationId_UserId",
            table: "ClinicOrganizationMemberships");

        migrationBuilder.CreateIndex(
            name: "IX_ClinicOrganizationMemberships_OrganizationId_UserId",
            table: "ClinicOrganizationMemberships",
            columns: new[] { "OrganizationId", "UserId" },
            unique: true,
            filter: "[IsRevoked] = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (
                SELECT 1 FROM dbo.ClinicOrganizationMemberships
                GROUP BY OrganizationId, UserId HAVING COUNT(*) > 1
            )
                THROW 51000, 'Cannot restore global membership uniqueness while reinvitation history exists.', 1;
            """);

        migrationBuilder.DropIndex(
            name: "IX_ClinicOrganizationMemberships_OrganizationId_UserId",
            table: "ClinicOrganizationMemberships");

        migrationBuilder.CreateIndex(
            name: "IX_ClinicOrganizationMemberships_OrganizationId_UserId",
            table: "ClinicOrganizationMemberships",
            columns: new[] { "OrganizationId", "UserId" },
            unique: true);
    }
}
