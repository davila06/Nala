using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RedeemedPromotionCodeId",
                table: "Subscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromotionCodeRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodeRedemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<int>(type: "int", nullable: true),
                    FreeMonths = table.Column<int>(type: "int", nullable: true),
                    TargetTier = table.Column<int>(type: "int", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "int", nullable: false),
                    RedeemedCount = table.Column<int>(type: "int", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodeRedemptions_PromotionCodeId",
                table: "PromotionCodeRedemptions",
                column: "PromotionCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodeRedemptions_UserId_PromotionCodeId",
                table: "PromotionCodeRedemptions",
                columns: new[] { "UserId", "PromotionCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodes_Code",
                table: "PromotionCodes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromotionCodeRedemptions");

            migrationBuilder.DropTable(
                name: "PromotionCodes");

            migrationBuilder.DropColumn(
                name: "RedeemedPromotionCodeId",
                table: "Subscriptions");
        }
    }
}
