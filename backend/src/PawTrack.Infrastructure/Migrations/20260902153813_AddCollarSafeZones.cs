using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarSafeZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollarSafeZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PolygonJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastKnownInside = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarSafeZones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollarSafeZones_CollarId_Enabled",
                table: "CollarSafeZones",
                columns: new[] { "CollarId", "Enabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollarSafeZones");
        }
    }
}
