using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalConsultations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicalConsultations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Subjective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Objective = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Assessment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Plan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TemperatureC = table.Column<decimal>(type: "decimal(4,1)", nullable: true),
                    HeartRateBpm = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRateRpm = table.Column<int>(type: "int", nullable: true),
                    BodyConditionScore = table.Column<int>(type: "int", nullable: true),
                    PainScore = table.Column<int>(type: "int", nullable: true),
                    HydrationStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Diagnosis = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Treatment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OwnerSummary = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignedByName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalConsultations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalConsultations_AppointmentId",
                table: "ClinicalConsultations",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalConsultations_ClinicId_PetId_CreatedAt",
                table: "ClinicalConsultations",
                columns: new[] { "ClinicId", "PetId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalConsultations");
        }
    }
}
