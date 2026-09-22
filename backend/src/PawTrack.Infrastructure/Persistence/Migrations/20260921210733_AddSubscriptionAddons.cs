using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAddons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionAddons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntitlementKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Units = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionAddons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAddons_SubscriptionId_EntitlementKey_StartsAt_ExpiresAt",
                table: "SubscriptionAddons",
                columns: new[] { "SubscriptionId", "EntitlementKey", "StartsAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionAddons_SubscriptionId_IsActive",
                table: "SubscriptionAddons",
                columns: new[] { "SubscriptionId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionAddons");
        }
    }
}
