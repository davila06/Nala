using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreLocationsAndOrderAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "StoreOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoreLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    Lng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreOrders_LocationId",
                table: "StoreOrders",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_StoreLocations_StoreId_IsActive",
                table: "StoreLocations",
                columns: new[] { "StoreId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreLocations");

            migrationBuilder.DropIndex(
                name: "IX_StoreOrders_LocationId",
                table: "StoreOrders");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "StoreOrders");
        }
    }
}
