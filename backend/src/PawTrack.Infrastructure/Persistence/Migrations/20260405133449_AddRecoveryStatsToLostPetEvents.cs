using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecoveryStatsToLostPetEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CantonName",
                table: "LostPetEvents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RecoveryDistanceMeters",
                table: "LostPetEvents",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "RecoveryTime",
                table: "LostPetEvents",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReunionLat",
                table: "LostPetEvents",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReunionLng",
                table: "LostPetEvents",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LostPetEvents_CantonName",
                table: "LostPetEvents",
                column: "CantonName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LostPetEvents_CantonName",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "CantonName",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "RecoveryDistanceMeters",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "RecoveryTime",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "ReunionLat",
                table: "LostPetEvents");

            migrationBuilder.DropColumn(
                name: "ReunionLng",
                table: "LostPetEvents");
        }
    }
}
