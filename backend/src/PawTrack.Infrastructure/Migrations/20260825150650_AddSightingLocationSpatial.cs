using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSightingLocationSpatial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Sightings",
                type: "geography",
                nullable: true);

            // Backfill from existing Lat/Lng columns.
            migrationBuilder.Sql(
                "UPDATE Sightings SET Location = geography::Point(Lat, Lng, 4326) WHERE Lat <> 0 OR Lng <> 0;");

            // Spatial index — enables index seeks for STDistance() radius queries.
            migrationBuilder.Sql(
                "CREATE SPATIAL INDEX IX_Sightings_Location ON Sightings(Location) " +
                "USING GEOGRAPHY_GRID WITH (GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Sightings");
        }
    }
}
