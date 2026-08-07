using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalWeightAndMedicationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DosageDescription",
                table: "MedicalRecords",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                table: "MedicalRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "MedicalRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "MedicationEndDate",
                table: "MedicalRecords",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "MedicalRecords",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DosageDescription",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "MedicationEndDate",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "MedicalRecords");
        }
    }
}
