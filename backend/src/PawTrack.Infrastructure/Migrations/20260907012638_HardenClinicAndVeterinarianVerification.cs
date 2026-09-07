using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenClinicAndVeterinarianVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "ClinicVeterinarians",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ExpiresAt",
                table: "ClinicVeterinarians",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ClinicVeterinarians",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "ClinicVeterinarians",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "ClinicVeterinarians",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminUserId",
                table: "ClinicVeterinarians",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImageUrl",
                table: "ClinicVeterinarians",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ClinicVeterinarians",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PendingReview");

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "ClinicVeterinarians",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                table: "ClinicVeterinarians",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentUrl",
                table: "ClinicVerifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RevalidationRequestedAt",
                table: "ClinicVerifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNotes",
                table: "ClinicVerifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "ClinicVerifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminUserId",
                table: "ClinicVerifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "ClinicVerifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SupersededAt",
                table: "ClinicVerifications",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ClinicVeterinarians
                SET Status = CASE
                        WHEN RevokedAt IS NOT NULL THEN N'Revoked'
                        WHEN CanIssueCertificates = 1 THEN N'Authorized'
                        ELSE N'PendingReview'
                    END,
                    DocumentUrl = CASE
                        WHEN CanIssueCertificates = 1 AND DocumentUrl IS NULL THEN N'legacy://sprint1-authorized'
                        ELSE DocumentUrl
                    END,
                    ExpiresAt = CASE
                        WHEN CanIssueCertificates = 1 AND ExpiresAt IS NULL THEN CONVERT(date, DATEADD(year, 1, SYSUTCDATETIME()))
                        ELSE ExpiresAt
                    END
                """);

            migrationBuilder.DropColumn(
                name: "CanIssueCertificates",
                table: "ClinicVeterinarians");

            migrationBuilder.CreateTable(
                name: "VerificationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVeterinarians_ClinicId_Status",
                table: "ClinicVeterinarians",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVeterinarians_ExpiresAt",
                table: "ClinicVeterinarians",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicVerifications_SubmittedAt",
                table: "ClinicVerifications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationAuditLogs_Action",
                table: "VerificationAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationAuditLogs_ActorUserId",
                table: "VerificationAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationAuditLogs_EntityType_EntityId_CreatedAt",
                table: "VerificationAuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_ClinicVeterinarians_ClinicId_Status",
                table: "ClinicVeterinarians");

            migrationBuilder.DropIndex(
                name: "IX_ClinicVeterinarians_ExpiresAt",
                table: "ClinicVeterinarians");

            migrationBuilder.DropIndex(
                name: "IX_ClinicVerifications_SubmittedAt",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminUserId",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "SignatureImageUrl",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                table: "ClinicVeterinarians");

            migrationBuilder.DropColumn(
                name: "DocumentUrl",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "RevalidationRequestedAt",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminUserId",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "ClinicVerifications");

            migrationBuilder.DropColumn(
                name: "SupersededAt",
                table: "ClinicVerifications");

            migrationBuilder.AddColumn<bool>(
                name: "CanIssueCertificates",
                table: "ClinicVeterinarians",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
