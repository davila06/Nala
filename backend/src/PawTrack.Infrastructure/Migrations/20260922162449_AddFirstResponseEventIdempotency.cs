using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirstResponseEventIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_EventName_CorrelationId",
                table: "ProductEvents",
                columns: new[] { "EventName", "CorrelationId" },
                unique: true,
                filter: "[EventName] = N'FirstResponseRecorded' AND [CorrelationId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductEvents_EventName_CorrelationId",
                table: "ProductEvents");
        }
    }
}
