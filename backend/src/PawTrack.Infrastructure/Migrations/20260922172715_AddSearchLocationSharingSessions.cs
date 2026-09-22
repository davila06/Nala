using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchLocationSharingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchLocationSharingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LostEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPrecise = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StoppedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchLocationSharingSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchLocationSharingSessions_ExpiresAt_ExpiredAt_StoppedAt",
                table: "SearchLocationSharingSessions",
                columns: new[] { "ExpiresAt", "ExpiredAt", "StoppedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchLocationSharingSessions_LostEventId_ConnectionId_IsPrecise",
                table: "SearchLocationSharingSessions",
                columns: new[] { "LostEventId", "ConnectionId", "IsPrecise" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchLocationSharingSessions");
        }
    }
}
