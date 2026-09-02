using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarLostMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLost",
                table: "Collars",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LostModeActivatedAt",
                table: "Collars",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LostPetEventId",
                table: "Collars",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLost",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "LostModeActivatedAt",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "LostPetEventId",
                table: "Collars");
        }
    }
}
