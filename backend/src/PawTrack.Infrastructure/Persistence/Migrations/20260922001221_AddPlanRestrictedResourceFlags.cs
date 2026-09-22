using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanRestrictedResourceFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PlanRestricted",
                table: "StoreProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PlanRestricted",
                table: "StoreLocations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PlanRestricted",
                table: "ProviderServices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanRestricted",
                table: "StoreProducts");

            migrationBuilder.DropColumn(
                name: "PlanRestricted",
                table: "StoreLocations");

            migrationBuilder.DropColumn(
                name: "PlanRestricted",
                table: "ProviderServices");
        }
    }
}
