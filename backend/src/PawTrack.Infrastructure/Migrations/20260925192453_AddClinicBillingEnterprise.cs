using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicBillingEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicCashCloses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicCashCloses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DiscountReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DiscountApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    VoidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicCashClosePaymentSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CashCloseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicCashClosePaymentSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicCashClosePaymentSnapshots_ClinicCashCloses_CashCloseId",
                        column: x => x.CashCloseId,
                        principalTable: "ClinicCashCloses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSaleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPriceCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    LineTotalCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InventoryLotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSaleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicSaleLines_ClinicSales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "ClinicSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicSalePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSalePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicSalePayments_ClinicSales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "ClinicSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCashClosePaymentSnapshots_CashCloseId",
                table: "ClinicCashClosePaymentSnapshots",
                column: "CashCloseId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCashCloses_ClinicId_BusinessDate",
                table: "ClinicCashCloses",
                columns: new[] { "ClinicId", "BusinessDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSaleLines_SaleId",
                table: "ClinicSaleLines",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSalePayments_ClinicId_ReceivedAt",
                table: "ClinicSalePayments",
                columns: new[] { "ClinicId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSalePayments_SaleId",
                table: "ClinicSalePayments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSales_ClinicId_ReceiptNumber",
                table: "ClinicSales",
                columns: new[] { "ClinicId", "ReceiptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicCashClosePaymentSnapshots");

            migrationBuilder.DropTable(
                name: "ClinicSaleLines");

            migrationBuilder.DropTable(
                name: "ClinicSalePayments");

            migrationBuilder.DropTable(
                name: "ClinicCashCloses");

            migrationBuilder.DropTable(
                name: "ClinicSales");
        }
    }
}
