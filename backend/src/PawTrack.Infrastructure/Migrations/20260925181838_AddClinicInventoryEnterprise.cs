using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicInventoryEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicInventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MinimumStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInventoryLots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpiresAt = table.Column<DateOnly>(type: "date", nullable: true),
                    InitialQuantity = table.Column<int>(type: "int", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                    UnitCostCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInventoryLots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicInventoryMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDelta = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CertificateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicInventoryMovements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryItems_ClinicId_Type_Name",
                table: "ClinicInventoryItems",
                columns: new[] { "ClinicId", "Type", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryLots_ClinicId_ItemId_LotNumber",
                table: "ClinicInventoryLots",
                columns: new[] { "ClinicId", "ItemId", "LotNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryLots_ItemId_AvailableQuantity_ExpiresAt",
                table: "ClinicInventoryLots",
                columns: new[] { "ItemId", "AvailableQuantity", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryMovements_CertificateId",
                table: "ClinicInventoryMovements",
                column: "CertificateId",
                filter: "[CertificateId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryMovements_ClinicId_ItemId_CreatedAt",
                table: "ClinicInventoryMovements",
                columns: new[] { "ClinicId", "ItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryMovements_ConsultationId",
                table: "ClinicInventoryMovements",
                column: "ConsultationId",
                filter: "[ConsultationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicInventoryItems");

            migrationBuilder.DropTable(
                name: "ClinicInventoryLots");

            migrationBuilder.DropTable(
                name: "ClinicInventoryMovements");
        }
    }
}
