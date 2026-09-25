using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicStaffMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicStaffMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicStaffMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicStaffMemberships_ClinicId_IsRevoked",
                table: "ClinicStaffMemberships",
                columns: new[] { "ClinicId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicStaffMemberships_ClinicId_UserId",
                table: "ClinicStaffMemberships",
                columns: new[] { "ClinicId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicStaffMemberships");
        }
    }
}
