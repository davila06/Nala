using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessWhatsAppContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWhatsAppContactEnabled",
                table: "Stores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWhatsAppContactEnabled",
                table: "ServiceProviders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "ServiceProviders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWhatsAppContactEnabled",
                table: "Clinics",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsWhatsAppContactEnabled",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "IsWhatsAppContactEnabled",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "ServiceProviders");

            migrationBuilder.DropColumn(
                name: "IsWhatsAppContactEnabled",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Clinics");
        }
    }
}
