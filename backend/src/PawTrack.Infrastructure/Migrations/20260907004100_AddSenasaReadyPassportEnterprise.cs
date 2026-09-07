using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSenasaReadyPassportEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "VetCertificates",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevokedAt",
                table: "VetCertificates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RevokedByUserId",
                table: "VetCertificates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificateAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseNumberSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    VerifiedByAdminUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpiresAt = table.Column<DateOnly>(type: "date", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicVeterinarians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CanIssueCertificates = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicVeterinarians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaccinePassports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuingClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssuingVeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PetSpeciesSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PetBreedSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PetSexSnapshot = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PetColorSnapshot = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    MicrochipSnapshot = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    OwnerNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ClinicNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClinicLicenseSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VetNameSnapshot = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VetLicenseSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    VerificationCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    FormatLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParasiteControl_ProductName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ParasiteControl_ApplicationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ParasiteControl_NextDueDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinePassports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VaccinePassportVaccines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LotNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ApplicationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    VaccinePassportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccinePassportVaccines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccinePassportVaccines_VaccinePassports_VaccinePassportId",
                        column: x => x.VaccinePassportId,
                        principalTable: "VaccinePassports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_Action",
                table: "CertificateAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditLogs_CertificateId_CreatedAt",
                table: "CertificateAuditLogs",
                columns: new[] { "CertificateId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVerifications_ClinicId_Status",
                table: "ClinicVerifications",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVerifications_ExpiresAt",
                table: "ClinicVerifications",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVeterinarians_ClinicId",
                table: "ClinicVeterinarians",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVeterinarians_ClinicId_LicenseNumber",
                table: "ClinicVeterinarians",
                columns: new[] { "ClinicId", "LicenseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinePassports_CertificateId",
                table: "VaccinePassports",
                column: "CertificateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinePassports_IssuingClinicId_IssuedAt",
                table: "VaccinePassports",
                columns: new[] { "IssuingClinicId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VaccinePassports_PetId_IssuedAt",
                table: "VaccinePassports",
                columns: new[] { "PetId", "IssuedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VaccinePassports_VerificationCode",
                table: "VaccinePassports",
                column: "VerificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccinePassportVaccines_VaccinePassportId",
                table: "VaccinePassportVaccines",
                column: "VaccinePassportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateAuditLogs");

            migrationBuilder.DropTable(
                name: "ClinicVerifications");

            migrationBuilder.DropTable(
                name: "ClinicVeterinarians");

            migrationBuilder.DropTable(
                name: "VaccinePassportVaccines");

            migrationBuilder.DropTable(
                name: "VaccinePassports");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "VetCertificates");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "VetCertificates");

            migrationBuilder.DropColumn(
                name: "RevokedByUserId",
                table: "VetCertificates");
        }
    }
}
