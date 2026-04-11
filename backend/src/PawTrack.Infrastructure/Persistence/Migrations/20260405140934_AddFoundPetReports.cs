using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFoundPetReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FoundPetReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoundSpecies = table.Column<int>(type: "int", nullable: false),
                    ColorDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SizeEstimate = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FoundLat = table.Column<double>(type: "float", nullable: false),
                    FoundLng = table.Column<double>(type: "float", nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MatchedLostPetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MatchScore = table.Column<int>(type: "int", nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoundPetReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FoundPetReports_LatLng",
                table: "FoundPetReports",
                columns: new[] { "FoundLat", "FoundLng" });

            migrationBuilder.CreateIndex(
                name: "IX_FoundPetReports_ReportedAt",
                table: "FoundPetReports",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FoundPetReports_Status",
                table: "FoundPetReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FoundPetReports");
        }
    }
}
