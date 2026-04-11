using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MicrochipId",
                table: "Pets",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchedPetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScanInput = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InputType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScannedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicScans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_MicrochipId",
                table: "Pets",
                column: "MicrochipId",
                unique: true,
                filter: "[MicrochipId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_LicenseNumber",
                table: "Clinics",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Status",
                table: "Clinics",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_UserId",
                table: "Clinics",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicScans_ClinicId",
                table: "ClinicScans",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicScans_ClinicId_ScannedAt",
                table: "ClinicScans",
                columns: new[] { "ClinicId", "ScannedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clinics");

            migrationBuilder.DropTable(
                name: "ClinicScans");

            migrationBuilder.DropIndex(
                name: "IX_Pets_MicrochipId",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MicrochipId",
                table: "Pets");
        }
    }
}
