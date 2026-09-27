using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthTimelineReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VetCertificates_PetId_IssuedAt_Id",
                table: "VetCertificates",
                columns: new[] { "PetId", "IssuedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderBookings_CustomerUserId_PetId_StartsAt",
                table: "ProviderBookings",
                columns: new[] { "CustomerUserId", "PetId", "StartsAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PetId_IsSuperseded_Date_Id",
                table: "MedicalRecords",
                columns: new[] { "PetId", "IsSuperseded", "Date", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VetCertificates_PetId_IssuedAt_Id",
                table: "VetCertificates");

            migrationBuilder.DropIndex(
                name: "IX_ProviderBookings_CustomerUserId_PetId_StartsAt",
                table: "ProviderBookings");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PetId_IsSuperseded_Date_Id",
                table: "MedicalRecords");
        }
    }
}
