using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVeterinarianAppointmentsAndPermissionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "ClinicVeterinarians",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "[\"medical:read\",\"medical:write\",\"certificates:issue\"]");

            migrationBuilder.CreateTable(
                name: "VeterinarianAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeterinarianAppointments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VeterinarianAppointments_ClinicId_VeterinarianId_StartsAt_Status",
                table: "VeterinarianAppointments",
                columns: new[] { "ClinicId", "VeterinarianId", "StartsAt", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeterinarianAppointments");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "ClinicVeterinarians");
        }
    }
}
