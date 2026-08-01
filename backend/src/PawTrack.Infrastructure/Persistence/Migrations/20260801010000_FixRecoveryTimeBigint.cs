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
            // time(7) overflows for multi-day recoveries (> 23 h 59 m).
            // Recreate as bigint storing TimeSpan.Ticks via EF value converter.
            migrationBuilder.AddColumn<long>(
                name: "RecoveryTimeTicks",
                table: "LostPetEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [LostPetEvents] SET [RecoveryTimeTicks] = " +
                "DATEDIFF_BIG(ns, CAST('00:00:00' AS time), [RecoveryTime]) / 100 " +
                "WHERE [RecoveryTime] IS NOT NULL");

            migrationBuilder.DropColumn(name: "RecoveryTime", table: "LostPetEvents");

            migrationBuilder.RenameColumn(
                name: "RecoveryTimeTicks",
                table: "LostPetEvents",
                newName: "RecoveryTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "RecoveryTimeOld",
                table: "LostPetEvents",
                type: "time",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [LostPetEvents] SET [RecoveryTimeOld] = " +
                "CAST(CAST([RecoveryTime] / 10000000.0 AS float) / 86400.0 AS time) " +
                "WHERE [RecoveryTime] IS NOT NULL AND [RecoveryTime] < 864000000000");

            migrationBuilder.DropColumn(name: "RecoveryTime", table: "LostPetEvents");

            migrationBuilder.RenameColumn(
                name: "RecoveryTimeOld",
                table: "LostPetEvents",
                newName: "RecoveryTime");
        }
    }
}
