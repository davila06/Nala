using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreOrderIdempotencyAndProviderRefundAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "StoreOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                table: "StoreOrders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReference",
                table: "ProviderPayments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmountCrc",
                table: "ProviderPayments",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrders_CustomerId_IdempotencyKey",
                table: "StoreOrders",
                columns: new[] { "CustomerId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreOrders_CustomerId_IdempotencyKey",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "RefundReference",
                table: "ProviderPayments");

            migrationBuilder.DropColumn(
                name: "RefundedAmountCrc",
                table: "ProviderPayments");
        }
    }
}
