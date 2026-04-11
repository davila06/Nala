using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofencingAdvanced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursEnd",
                table: "UserLocations",
                type: "time(0)",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "QuietHoursStart",
                table: "UserLocations",
                type: "time(0)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActionConfirmedAt",
                table: "Notifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionSummary",
                table: "Notifications",
                type: "nvarchar(280)",
                maxLength: 280,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardAmount",
                table: "LostPetEvents",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RewardNote",
                table: "LostPetEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllyProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AllyType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CoverageLabel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CoverageLat = table.Column<double>(type: "float", nullable: false),
                    CoverageLng = table.Column<double>(type: "float", nullable: false),
                    CoverageRadiusMetres = table.Column<int>(type: "int", nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllyProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "GeofencedAlertLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LostPetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofencedAlertLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllyProfiles_VerificationStatus",
                table: "AllyProfiles",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_GeofencedAlertLogs_UserId_LostPetEventId",
                table: "GeofencedAlertLogs",
                columns: new[] { "UserId", "LostPetEventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllyProfiles");

            migrationBuilder.DropTable(
                name: "GeofencedAlertLogs");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "QuietHoursEnd",
                table: "UserLocations");

            migrationBuilder.DropColumn(
                name: "QuietHoursStart",
                table: "UserLocations");

            migrationBuilder.DropColumn(
                name: "ActionConfirmedAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ActionSummary",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RewardAmount",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "RewardNote",
                table: "LostPetEvents");
        }
    }
}
