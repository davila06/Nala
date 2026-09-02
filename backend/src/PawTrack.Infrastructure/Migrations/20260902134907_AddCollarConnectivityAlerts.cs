using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarConnectivityAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatteryAlertThresholdPercent",
                table: "Collars",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "BatteryAlertsEnabled",
                table: "Collars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOffline",
                table: "Collars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OfflineAlertsEnabled",
                table: "Collars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OfflineThresholdMinutes",
                table: "Collars",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryAlertThresholdPercent",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "BatteryAlertsEnabled",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "IsOffline",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "OfflineAlertsEnabled",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "OfflineThresholdMinutes",
                table: "Collars");
        }
    }
}
