using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicMedicalAccessAuditDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessMethod",
                table: "ClinicMedicalAccessLogs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "unknown");

            migrationBuilder.AddColumn<string>(
                name: "Operation",
                table: "ClinicMedicalAccessLogs",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "read_medical_history");

            migrationBuilder.AddColumn<string>(
                name: "Outcome",
                table: "ClinicMedicalAccessLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "allowed");

            migrationBuilder.AddColumn<string>(
                name: "Permission",
                table: "ClinicMedicalAccessLogs",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "read");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ClinicMedicalAccessLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessMethod",
                table: "ClinicMedicalAccessLogs");

            migrationBuilder.DropColumn(
                name: "Operation",
                table: "ClinicMedicalAccessLogs");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "ClinicMedicalAccessLogs");

            migrationBuilder.DropColumn(
                name: "Permission",
                table: "ClinicMedicalAccessLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ClinicMedicalAccessLogs");
        }
    }
}
