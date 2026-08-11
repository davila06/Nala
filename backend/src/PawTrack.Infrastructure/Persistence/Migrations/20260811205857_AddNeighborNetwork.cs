using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNeighborNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NeighborAlerts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EnrolledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NeighborAlerts", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NeighborAlerts_IsActive_Lat_Lng",
                table: "NeighborAlerts",
                columns: new[] { "IsActive", "Lat", "Lng" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NeighborAlerts");
        }
    }
}
