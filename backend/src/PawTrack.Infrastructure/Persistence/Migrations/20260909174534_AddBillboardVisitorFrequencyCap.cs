using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillboardVisitorFrequencyCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VisitorHash",
                table: "BillboardDeliveryEvents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_BillboardDeliveryEvents_BillboardId_VisitorHash_OccurredOn_EventType",
                table: "BillboardDeliveryEvents",
                columns: new[] { "BillboardId", "VisitorHash", "OccurredOn", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillboardDeliveryEvents_BillboardId_VisitorHash_OccurredOn_EventType",
                table: "BillboardDeliveryEvents");

            migrationBuilder.DropColumn(
                name: "VisitorHash",
                table: "BillboardDeliveryEvents");
        }
    }
}
