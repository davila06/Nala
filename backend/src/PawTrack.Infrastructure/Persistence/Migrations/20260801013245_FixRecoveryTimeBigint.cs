using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRecoveryTimeBigint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot ALTER a `time` column directly to `bigint` — drop and recreate.
            migrationBuilder.DropColumn(name: "RecoveryTime", table: "LostPetEvents");
            migrationBuilder.AddColumn<long>(
                name: "RecoveryTime",
                table: "LostPetEvents",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RecoveryTime", table: "LostPetEvents");
            migrationBuilder.AddColumn<TimeSpan>(
                name: "RecoveryTime",
                table: "LostPetEvents",
                type: "time",
                nullable: true);
        }
    }
}
