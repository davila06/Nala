using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarHandoverCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollarHandoverCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedByOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RedeemedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarHandoverCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollarHandoverCodes_CollarId_RedeemedAt_CancelledAt",
                table: "CollarHandoverCodes",
                columns: new[] { "CollarId", "RedeemedAt", "CancelledAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollarHandoverCodes");
        }
    }
}
