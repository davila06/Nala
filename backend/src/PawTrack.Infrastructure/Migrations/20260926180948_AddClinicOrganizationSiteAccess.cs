using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PawTrack.Infrastructure.Persistence;

#nullable disable

namespace PawTrack.Infrastructure.Migrations;

[DbContext(typeof(PawTrackDbContext))]
[Migration("20260926180948_AddClinicOrganizationSiteAccess")]
public sealed class AddClinicOrganizationSiteAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ClinicOrganizationSiteAccess",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClinicOrganizationSiteAccess", x => x.Id);
                table.ForeignKey("FK_ClinicOrganizationSiteAccess_ClinicOrganizations_OrganizationId", x => x.OrganizationId, "ClinicOrganizations", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ClinicOrganizationSiteAccess_Clinics_ClinicId", x => x.ClinicId, "Clinics", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ClinicOrganizationSiteAccess_Users_GrantedByUserId", x => x.GrantedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_ClinicOrganizationSiteAccess_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ClinicOrganizationSiteAccess_ClinicId", "ClinicOrganizationSiteAccess", "ClinicId");
        migrationBuilder.CreateIndex("IX_ClinicOrganizationSiteAccess_GrantedByUserId", "ClinicOrganizationSiteAccess", "GrantedByUserId");
        migrationBuilder.CreateIndex("IX_ClinicOrganizationSiteAccess_OrganizationId", "ClinicOrganizationSiteAccess", "OrganizationId");
        migrationBuilder.CreateIndex(
            "IX_ClinicOrganizationSiteAccess_UserId_ClinicId_IsRevoked",
            "ClinicOrganizationSiteAccess",
            new[] { "UserId", "ClinicId", "IsRevoked" });
        migrationBuilder.CreateIndex(
            "IX_ClinicOrganizationSiteAccess_Active_UserId_ClinicId",
            "ClinicOrganizationSiteAccess",
            new[] { "UserId", "ClinicId" },
            unique: true,
            filter: "[IsRevoked] = 0");

        migrationBuilder.Sql("""
            INSERT INTO dbo.ClinicOrganizationMemberships
                (Id, OrganizationId, UserId, Role, IsRevoked, GrantedAt, RevokedAt)
            SELECT NEWID(), site.OrganizationId, member.UserId, N'Member', 0, SYSUTCDATETIME(), NULL
            FROM dbo.ClinicOrganizationSites site
            INNER JOIN dbo.ClinicStaffMemberships member ON member.ClinicId = site.ClinicId
            WHERE member.IsRevoked = 0
              AND NOT EXISTS
              (
                  SELECT 1 FROM dbo.ClinicOrganizationMemberships existing
                  WHERE existing.OrganizationId = site.OrganizationId
                    AND existing.UserId = member.UserId
                    AND existing.IsRevoked = 0
              )
            UNION
            SELECT NEWID(), site.OrganizationId, member.UserId, N'Member', 0, SYSUTCDATETIME(), NULL
            FROM dbo.ClinicOrganizationSites site
            INNER JOIN dbo.ClinicFinanceMemberships member ON member.ClinicId = site.ClinicId
            WHERE member.IsRevoked = 0
              AND NOT EXISTS
              (
                  SELECT 1 FROM dbo.ClinicOrganizationMemberships existing
                  WHERE existing.OrganizationId = site.OrganizationId
                    AND existing.UserId = member.UserId
                    AND existing.IsRevoked = 0
              );

            INSERT INTO dbo.ClinicOrganizationSiteAccess
                (Id, OrganizationId, ClinicId, UserId, GrantedByUserId, IsRevoked, GrantedAt, RevokedAt)
            SELECT NEWID(), site.OrganizationId, site.ClinicId, membership.UserId, clinic.UserId, 0, SYSUTCDATETIME(), NULL
            FROM dbo.ClinicOrganizationSites site
            INNER JOIN dbo.Clinics clinic ON clinic.Id = site.ClinicId
            INNER JOIN dbo.ClinicOrganizationMemberships membership ON membership.OrganizationId = site.OrganizationId
            WHERE membership.Role = N'Owner' AND membership.IsRevoked = 0 AND site.IsPrimary = 1
            UNION
            SELECT NEWID(), site.OrganizationId, site.ClinicId, member.UserId, clinic.UserId, 0, SYSUTCDATETIME(), NULL
            FROM dbo.ClinicOrganizationSites site
            INNER JOIN dbo.Clinics clinic ON clinic.Id = site.ClinicId
            INNER JOIN dbo.ClinicStaffMemberships member ON member.ClinicId = site.ClinicId AND member.IsRevoked = 0
            INNER JOIN dbo.ClinicOrganizationMemberships membership
                ON membership.OrganizationId = site.OrganizationId
               AND membership.UserId = member.UserId
               AND membership.IsRevoked = 0
            UNION
            SELECT NEWID(), site.OrganizationId, site.ClinicId, member.UserId, clinic.UserId, 0, SYSUTCDATETIME(), NULL
            FROM dbo.ClinicOrganizationSites site
            INNER JOIN dbo.Clinics clinic ON clinic.Id = site.ClinicId
            INNER JOIN dbo.ClinicFinanceMemberships member ON member.ClinicId = site.ClinicId AND member.IsRevoked = 0
            INNER JOIN dbo.ClinicOrganizationMemberships membership
                ON membership.OrganizationId = site.OrganizationId
               AND membership.UserId = member.UserId
               AND membership.IsRevoked = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS
            (
                SELECT 1
                FROM dbo.ClinicOrganizationSiteAccess access
                WHERE access.IsRevoked = 0
            )
                THROW 51000, 'Cannot downgrade active organization site access without exporting or consolidating it first.', 1;
            """);
        migrationBuilder.DropTable(name: "ClinicOrganizationSiteAccess");
    }
}
