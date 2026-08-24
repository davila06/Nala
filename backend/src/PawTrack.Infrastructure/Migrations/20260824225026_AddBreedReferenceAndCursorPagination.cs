using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBreedReferenceAndCursorPagination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BreedReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BreedKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WeightMinKg = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WeightMaxKg = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    WeightLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ActivityMinMinutes = table.Column<int>(type: "int", nullable: true),
                    ActivityMaxMinutes = table.Column<int>(type: "int", nullable: true),
                    ActivityMinKm = table.Column<int>(type: "int", nullable: true),
                    ActivityMaxKm = table.Column<int>(type: "int", nullable: true),
                    EnergyLevel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsSpeciesFallback = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreedReferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BreedReferences_BreedKey_Species",
                table: "BreedReferences",
                columns: new[] { "BreedKey", "Species" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BreedReferences_Species_IsSpeciesFallback",
                table: "BreedReferences",
                columns: new[] { "Species", "IsSpeciesFallback" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreedReferences");
        }
    }
}
