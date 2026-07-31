using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificatesMunicipalitiesWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapturedAnimals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EstimatedAge = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CollarChipNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MatchedPetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapturedAnimals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VetCertificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VerificationCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PdfUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VetCertificates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapturedAnimals_Canton",
                table: "CapturedAnimals",
                column: "Canton");

            migrationBuilder.CreateIndex(
                name: "IX_CapturedAnimals_Canton_Status",
                table: "CapturedAnimals",
                columns: new[] { "Canton", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CapturedAnimals_CollarChipNumber",
                table: "CapturedAnimals",
                column: "CollarChipNumber");

            migrationBuilder.CreateIndex(
                name: "IX_VetCertificates_ClinicId_IssuedAt",
                table: "VetCertificates",
                columns: new[] { "ClinicId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VetCertificates_PetId",
                table: "VetCertificates",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_VetCertificates_VerificationCode",
                table: "VetCertificates",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapturedAnimals");

            migrationBuilder.DropTable(
                name: "VetCertificates");
        }
    }
}
