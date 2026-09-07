using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCancellationPolicySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationPolicySnapshot",
                table: "ProviderBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeCrc",
                table: "ProviderBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalCrc",
                table: "ProviderBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxCrc",
                table: "ProviderBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCrc",
                table: "ProviderBookings",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationPolicySnapshot",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "PlatformFeeCrc",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "SubtotalCrc",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "TaxCrc",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "TotalCrc",
                table: "ProviderBookings");
        }
    }
}
