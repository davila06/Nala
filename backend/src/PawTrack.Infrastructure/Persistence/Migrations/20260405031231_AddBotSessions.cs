using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBotSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PhoneNumberHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Step = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastSeenRaw = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LocationRaw = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LastSeenLat = table.Column<double>(type: "float", nullable: true),
                    LastSeenLng = table.Column<double>(type: "float", nullable: true),
                    GuestUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LostEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProcessedMessageIds = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "[]"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotSessions_ExpiresAt",
                table: "BotSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_BotSessions_PhoneNumberHash_Step",
                table: "BotSessions",
                columns: new[] { "PhoneNumberHash", "Step" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotSessions");
        }
    }
}
