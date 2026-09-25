using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicSaleRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicSaleRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RefundedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicSaleRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicSaleRefunds_ClinicSales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "ClinicSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSaleRefunds_ClinicId_EvidenceReference",
                table: "ClinicSaleRefunds",
                columns: new[] { "ClinicId", "EvidenceReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSaleRefunds_ClinicId_RefundedAt",
                table: "ClinicSaleRefunds",
                columns: new[] { "ClinicId", "RefundedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicSaleRefunds_SaleId",
                table: "ClinicSaleRefunds",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicSaleRefunds");
        }
    }
}
