using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarLocationTruthfulness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLocationRecordedAt",
                table: "Collars",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LastPositionOnline",
                table: "Collars",
                type: "bit",
                nullable: true);

            // Backfill: collars with a known last fix didn't previously distinguish device
            // timestamp from server receipt — LastSeenAt is the closest known approximation.
            migrationBuilder.Sql(
                "UPDATE [Collars] SET [LastLocationRecordedAt] = [LastSeenAt], [LastPositionOnline] = 1 " +
                "WHERE [LastSeenAt] IS NOT NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "CollarLocations",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceivedAt",
                table: "CollarLocations",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            // Backfill existing rows: before this migration, ReceivedAt didn't exist and every
            // fix was assumed processed at write time — the closest known approximation is the
            // point's own RecordedAt (both were previously stamped with the same UtcNow value).
            migrationBuilder.Sql("UPDATE [CollarLocations] SET [ReceivedAt] = [RecordedAt];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLocationRecordedAt",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "LastPositionOnline",
                table: "Collars");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "CollarLocations");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "CollarLocations");
        }
    }
}
