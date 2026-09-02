using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollarAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Serial = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Event = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollarAuditEntries_CollarId_CreatedAt",
                table: "CollarAuditEntries",
                columns: new[] { "CollarId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CollarAuditEntries_Serial_CreatedAt",
                table: "CollarAuditEntries",
                columns: new[] { "Serial", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollarAuditEntries");
        }
    }
}
