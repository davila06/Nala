using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicWidgetDomains : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicWidgetDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(253)", maxLength: 253, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicWidgetDomains", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicWidgetDomains_ClinicId_Domain",
                table: "ClinicWidgetDomains",
                columns: new[] { "ClinicId", "Domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicWidgetDomains_ClinicId_IsActive",
                table: "ClinicWidgetDomains",
                columns: new[] { "ClinicId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicWidgetDomains");
        }
    }
}
