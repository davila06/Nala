using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAnalyticsEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvertiserName",
                table: "Billboards",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BudgetCrc",
                table: "Billboards",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CampaignStatus",
                table: "Billboards",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Billboards",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContractReference",
                table: "Billboards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrequencyCapPerDay",
                table: "Billboards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCategoryExclusive",
                table: "Billboards",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "Billboards",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "Billboards",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "Billboards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetCanton",
                table: "Billboards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BillboardDeliveryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillboardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventKeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillboardDeliveryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SchemaVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AnonymousId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillboardDeliveryEvents_BillboardId_EventType_EventKeyHash",
                table: "BillboardDeliveryEvents",
                columns: new[] { "BillboardId", "EventType", "EventKeyHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillboardDeliveryEvents_BillboardId_OccurredOn_EventType",
                table: "BillboardDeliveryEvents",
                columns: new[] { "BillboardId", "OccurredOn", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_Canton_OccurredAt",
                table: "ProductEvents",
                columns: new[] { "Canton", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_EventId",
                table: "ProductEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_EventName_OccurredAt",
                table: "ProductEvents",
                columns: new[] { "EventName", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillboardDeliveryEvents");

            migrationBuilder.DropTable(
                name: "ProductEvents");

            migrationBuilder.DropColumn(
                name: "AdvertiserName",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "BudgetCrc",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "CampaignStatus",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "ContractReference",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "FrequencyCapPerDay",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "IsCategoryExclusive",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "Billboards");

            migrationBuilder.DropColumn(
                name: "TargetCanton",
                table: "Billboards");
        }
    }
}
