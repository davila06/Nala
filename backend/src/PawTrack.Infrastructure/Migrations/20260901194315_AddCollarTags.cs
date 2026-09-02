using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollarTagSerial",
                table: "Collars",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollarDeviceCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarDeviceCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollarTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Serial = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ManufacturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SoldAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastPingAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarTags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollarDeviceCredentials_KeyHash",
                table: "CollarDeviceCredentials",
                column: "KeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_CollarTags_Serial",
                table: "CollarTags",
                column: "Serial",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollarDeviceCredentials");

            migrationBuilder.DropTable(
                name: "CollarTags");

            migrationBuilder.DropColumn(
                name: "CollarTagSerial",
                table: "Collars");
        }
    }
}
