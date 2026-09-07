using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnimalWelfareCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnimalWelfareCaseAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalWelfareCaseAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimalWelfareCaseNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalWelfareCaseNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimalWelfareCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublicCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LostPetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SightingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CapturedAnimalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdoptablePetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ApproxLat = table.Column<double>(type: "float", nullable: true),
                    ApproxLng = table.Column<double>(type: "float", nullable: true),
                    DescriptionSanitized = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReporterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReporterIsAnonymous = table.Column<bool>(type: "bit", nullable: false),
                    AssignedOrganizationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedRole = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ClosureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalWelfareCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimalWelfareEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EvidenceKind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsSensitive = table.Column<bool>(type: "bit", nullable: false),
                    HashSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalWelfareEvidence", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimalWelfareReferrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ReferredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReferredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimalWelfareReferrals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCaseAuditLogs_Action",
                table: "AnimalWelfareCaseAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCaseAuditLogs_CaseId_CreatedAt",
                table: "AnimalWelfareCaseAuditLogs",
                columns: new[] { "CaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCaseNotes_CaseId_CreatedAt",
                table: "AnimalWelfareCaseNotes",
                columns: new[] { "CaseId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_AssignedOrganizationUserId_Status",
                table: "AnimalWelfareCases",
                columns: new[] { "AssignedOrganizationUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_Canton_Status",
                table: "AnimalWelfareCases",
                columns: new[] { "Canton", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_CapturedAnimalId",
                table: "AnimalWelfareCases",
                column: "CapturedAnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_PetId",
                table: "AnimalWelfareCases",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_PublicCode",
                table: "AnimalWelfareCases",
                column: "PublicCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_Severity_Status",
                table: "AnimalWelfareCases",
                columns: new[] { "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_Status_CreatedAt",
                table: "AnimalWelfareCases",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareEvidence_CaseId_UploadedAt",
                table: "AnimalWelfareEvidence",
                columns: new[] { "CaseId", "UploadedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareReferrals_CaseId_ReferredAt",
                table: "AnimalWelfareReferrals",
                columns: new[] { "CaseId", "ReferredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnimalWelfareCaseAuditLogs");

            migrationBuilder.DropTable(
                name: "AnimalWelfareCaseNotes");

            migrationBuilder.DropTable(
                name: "AnimalWelfareCases");

            migrationBuilder.DropTable(
                name: "AnimalWelfareEvidence");

            migrationBuilder.DropTable(
                name: "AnimalWelfareReferrals");
        }
    }
}
