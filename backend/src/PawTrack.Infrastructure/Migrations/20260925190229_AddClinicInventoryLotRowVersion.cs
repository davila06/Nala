using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicInventoryLotRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicInventoryLots_ClinicId_ItemId_LotNumber",
                table: "ClinicInventoryLots");

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "ClinicInventoryLots",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ClinicInventoryLots",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryLots_ClinicId_ItemId_LocationName_LotNumber",
                table: "ClinicInventoryLots",
                columns: new[] { "ClinicId", "ItemId", "LocationName", "LotNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicInventoryLots_ClinicId_ItemId_LocationName_LotNumber",
                table: "ClinicInventoryLots");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "ClinicInventoryLots");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ClinicInventoryLots");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicInventoryLots_ClinicId_ItemId_LotNumber",
                table: "ClinicInventoryLots",
                columns: new[] { "ClinicId", "ItemId", "LotNumber" });
        }
    }
}
