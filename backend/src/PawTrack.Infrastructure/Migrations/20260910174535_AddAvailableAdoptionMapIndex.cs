using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailableAdoptionMapIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AdoptableAnimals_Status_RefLat_RefLng",
                table: "AdoptableAnimals",
                columns: new[] { "Status", "RefLat", "RefLng" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdoptableAnimals_Status_RefLat_RefLng",
                table: "AdoptableAnimals");
        }
    }
}
