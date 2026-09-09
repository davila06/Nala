using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClinicEnterpriseWorkflowAndMedicalVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperseded",
                table: "MedicalRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAt",
                table: "MedicalRecords",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersededByUserId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupersedesRecordId",
                table: "MedicalRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupersessionReason",
                table: "MedicalRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MedicalRecords",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ClinicProfileChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedProfileJson = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicProfileChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PetId_IsSuperseded",
                table: "MedicalRecords",
                columns: new[] { "PetId", "IsSuperseded" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicProfileChanges_ClinicId_Status_CreatedAt",
                table: "ClinicProfileChanges",
                columns: new[] { "ClinicId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicProfileChanges");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PetId_IsSuperseded",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "IsSuperseded",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "SupersededAt",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "SupersededByUserId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "SupersedesRecordId",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "SupersessionReason",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MedicalRecords");
        }
    }
}
