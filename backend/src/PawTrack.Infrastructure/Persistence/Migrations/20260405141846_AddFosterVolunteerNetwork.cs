using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFosterVolunteerNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustodyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FosterUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FoundPetReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpectedDays = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FosterVolunteers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HomeLat = table.Column<double>(type: "float", nullable: false),
                    HomeLng = table.Column<double>(type: "float", nullable: false),
                    AcceptedSpeciesCsv = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SizePreference = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaxDays = table.Column<int>(type: "int", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    AvailableUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalFostersCompleted = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FosterVolunteers", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyRecords_FosterUserId",
                table: "CustodyRecords",
                column: "FosterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyRecords_FoundPetReportId",
                table: "CustodyRecords",
                column: "FoundPetReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyRecords_Status",
                table: "CustodyRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FosterVolunteers_HomeLatLng",
                table: "FosterVolunteers",
                columns: new[] { "HomeLat", "HomeLng" });

            migrationBuilder.CreateIndex(
                name: "IX_FosterVolunteers_IsAvailable",
                table: "FosterVolunteers",
                column: "IsAvailable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustodyRecords");

            migrationBuilder.DropTable(
                name: "FosterVolunteers");
        }
    }
}
