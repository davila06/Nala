using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicOrganizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicOrganizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicOrganizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicOrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicOrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicOrganizationMemberships_ClinicOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "ClinicOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClinicOrganizationMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicOrganizationSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicOrganizationSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicOrganizationSites_ClinicOrganizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "ClinicOrganizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicOrganizationSites_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOrganizationMemberships_OrganizationId_UserId",
                table: "ClinicOrganizationMemberships",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOrganizationMemberships_UserId_IsRevoked",
                table: "ClinicOrganizationMemberships",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOrganizationSites_ClinicId",
                table: "ClinicOrganizationSites",
                column: "ClinicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicOrganizationSites_OrganizationId_IsPrimary",
                table: "ClinicOrganizationSites",
                columns: new[] { "OrganizationId", "IsPrimary" });

            migrationBuilder.Sql("""
                CREATE TABLE #ClinicOrganizationBackfill
                (
                    ClinicId uniqueidentifier NOT NULL PRIMARY KEY,
                    OrganizationId uniqueidentifier NOT NULL,
                    UserId uniqueidentifier NOT NULL,
                    OrganizationName nvarchar(200) NOT NULL,
                    CreatedAt datetimeoffset NOT NULL
                );

                INSERT INTO #ClinicOrganizationBackfill (ClinicId, OrganizationId, UserId, OrganizationName, CreatedAt)
                SELECT Id, NEWID(), UserId, Name, RegisteredAt
                FROM dbo.Clinics;

                INSERT INTO dbo.ClinicOrganizations (Id, Name, CreatedAt)
                SELECT OrganizationId, OrganizationName, CreatedAt
                FROM #ClinicOrganizationBackfill;

                INSERT INTO dbo.ClinicOrganizationSites (Id, OrganizationId, ClinicId, IsPrimary, AddedAt)
                SELECT NEWID(), OrganizationId, ClinicId, 1, CreatedAt
                FROM #ClinicOrganizationBackfill;

                INSERT INTO dbo.ClinicOrganizationMemberships (Id, OrganizationId, UserId, Role, IsRevoked, GrantedAt, RevokedAt)
                SELECT NEWID(), OrganizationId, UserId, N'Owner', 0, CreatedAt, NULL
                FROM #ClinicOrganizationBackfill;

                DROP TABLE #ClinicOrganizationBackfill;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.ClinicOrganizations organization
                    WHERE (SELECT COUNT(*) FROM dbo.ClinicOrganizationSites site WHERE site.OrganizationId = organization.Id) <> 1
                       OR (SELECT COUNT(*) FROM dbo.ClinicOrganizationMemberships membership WHERE membership.OrganizationId = organization.Id) <> 1
                       OR NOT EXISTS
                          (
                              SELECT 1
                              FROM dbo.ClinicOrganizationSites site
                              INNER JOIN dbo.Clinics clinic ON clinic.Id = site.ClinicId
                              INNER JOIN dbo.ClinicOrganizationMemberships membership
                                  ON membership.OrganizationId = organization.Id
                              WHERE site.OrganizationId = organization.Id
                                AND membership.UserId = clinic.UserId
                                AND membership.Role = N'Owner'
                                AND membership.IsRevoked = 0
                          )
                )
                    THROW 51000, 'Cannot downgrade multisite organization data without exporting or consolidating it first.', 1;
                """);

            migrationBuilder.DropTable(
                name: "ClinicOrganizationMemberships");

            migrationBuilder.DropTable(
                name: "ClinicOrganizationSites");

            migrationBuilder.DropTable(
                name: "ClinicOrganizations");
        }
    }
}
