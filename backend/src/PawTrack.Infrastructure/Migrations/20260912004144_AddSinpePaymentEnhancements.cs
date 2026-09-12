using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSinpePaymentEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankReceiptNumber",
                table: "Subscriptions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QrScanEvents_ScannedAt",
                table: "QrScanEvents",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CollarLocations_RecordedAt",
                table: "CollarLocations",
                column: "RecordedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QrScanEvents_ScannedAt",
                table: "QrScanEvents");

            migrationBuilder.DropIndex(
                name: "IX_CollarLocations_RecordedAt",
                table: "CollarLocations");

            migrationBuilder.DropColumn(
                name: "BankReceiptNumber",
                table: "Subscriptions");
        }
    }
}
