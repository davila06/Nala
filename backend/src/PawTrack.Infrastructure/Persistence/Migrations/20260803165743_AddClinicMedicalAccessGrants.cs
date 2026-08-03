using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicMedicalAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicMedicalAccessGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetOwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitiatedBy = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CodeExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicMedicalAccessGrants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMedicalAccessGrants_ClinicId",
                table: "ClinicMedicalAccessGrants",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMedicalAccessGrants_ClinicId_PetId_IsActive",
                table: "ClinicMedicalAccessGrants",
                columns: new[] { "ClinicId", "PetId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMedicalAccessGrants_CodeHash",
                table: "ClinicMedicalAccessGrants",
                column: "CodeHash",
                filter: "[AcceptedAt] IS NULL AND [IsActive] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicMedicalAccessGrants_PetId",
                table: "ClinicMedicalAccessGrants",
                column: "PetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicMedicalAccessGrants");
        }
    }
}
