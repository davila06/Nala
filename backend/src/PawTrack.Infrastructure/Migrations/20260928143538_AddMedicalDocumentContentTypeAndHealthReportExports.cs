using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalDocumentContentTypeAndHealthReportExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthReportExports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BlobUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ItemCount = table.Column<int>(type: "int", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthReportExports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthReportExports_RequestedByUserId_PetId_RequestedAt",
                table: "HealthReportExports",
                columns: new[] { "RequestedByUserId", "PetId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthReportExports_Status_RequestedAt",
                table: "HealthReportExports",
                columns: new[] { "Status", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HealthReportExports_Status_StartedAt",
                table: "HealthReportExports",
                columns: new[] { "Status", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthReportExports");
        }
    }
}
