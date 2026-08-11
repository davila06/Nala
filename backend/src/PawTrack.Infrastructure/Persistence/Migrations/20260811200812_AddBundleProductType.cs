using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBundleProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmergencyPhone",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmergency24h",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProductType",
                table: "BundleOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmergencyPhone",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "IsEmergency24h",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "BundleOrders");
        }
    }
}
