using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseMarketplaceStockAndPaymentLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockOnHand",
                table: "StoreProducts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentConfirmedAt",
                table: "StoreOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentVerificationReference",
                table: "StoreOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentVerifiedByUserId",
                table: "StoreOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StockReservationExpiresAt",
                table: "StoreOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StockReserved",
                table: "StoreOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentIntentId",
                table: "ProviderPayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderPayments_PaymentIntentId",
                table: "ProviderPayments",
                column: "PaymentIntentId",
                unique: true,
                filter: "[PaymentIntentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderPayments_PaymentIntents_PaymentIntentId",
                table: "ProviderPayments",
                column: "PaymentIntentId",
                principalTable: "PaymentIntents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProviderPayments_PaymentIntents_PaymentIntentId",
                table: "ProviderPayments");

            migrationBuilder.DropIndex(
                name: "IX_ProviderPayments_PaymentIntentId",
                table: "ProviderPayments");

            migrationBuilder.DropColumn(
                name: "StockOnHand",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "PaymentConfirmedAt",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "PaymentVerificationReference",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "PaymentVerifiedByUserId",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "StockReservationExpiresAt",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "StockReserved",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "PaymentIntentId",
                table: "ProviderPayments");
        }
    }
}
