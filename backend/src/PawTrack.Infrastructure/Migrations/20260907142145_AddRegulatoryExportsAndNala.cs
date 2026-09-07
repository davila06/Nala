using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegulatoryExportsAndNala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegulatoryExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Format = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: true),
                    SuppressedRowCount = table.Column<int>(type: "int", nullable: true),
                    PayloadSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DownloadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatoryExports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulatorySubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SubmissionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulatorySubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    SuppressionThreshold = table.Column<int>(type: "int", nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_ExportCode",
                table: "RegulatoryExports",
                column: "ExportCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_OrganizationId_PeriodStart_PeriodEnd",
                table: "RegulatoryExports",
                columns: new[] { "OrganizationId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_ReportType_SchemaVersion",
                table: "RegulatoryExports",
                columns: new[] { "ReportType", "SchemaVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_RequestedByUserId_IdempotencyKey",
                table: "RegulatoryExports",
                columns: new[] { "RequestedByUserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_RequestedByUserId_RequestedAt",
                table: "RegulatoryExports",
                columns: new[] { "RequestedByUserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryExports_Status_RequestedAt",
                table: "RegulatoryExports",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatorySubmissions_ExportId_CreatedAt",
                table: "RegulatorySubmissions",
                columns: new[] { "ExportId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatorySubmissions_IdempotencyKey",
                table: "RegulatorySubmissions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegulatorySubmissions_Status_CreatedAt",
                table: "RegulatorySubmissions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_Code_SchemaVersion",
                table: "ReportDefinitions",
                columns: new[] { "Code", "SchemaVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_ReportType_Scope_IsActive",
                table: "ReportDefinitions",
                columns: new[] { "ReportType", "Scope", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulatoryExports");

            migrationBuilder.DropTable(
                name: "RegulatorySubmissions");

            migrationBuilder.DropTable(
                name: "ReportDefinitions");
        }
    }
}
