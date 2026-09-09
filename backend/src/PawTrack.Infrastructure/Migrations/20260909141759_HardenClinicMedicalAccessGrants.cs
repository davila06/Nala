using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenClinicMedicalAccessGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccessExpiresAt",
                table: "ClinicMedicalAccessGrants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Permissions",
                table: "ClinicMedicalAccessGrants",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "[\"read\",\"write\"]");

            migrationBuilder.AlterColumn<string>(
                name: "Scopes",
                table: "ClinicApiKeys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[\"scan\",\"medical:read\",\"medical:write\",\"certificates\",\"analytics\"]",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessExpiresAt",
                table: "ClinicMedicalAccessGrants");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "ClinicMedicalAccessGrants");

            migrationBuilder.AlterColumn<string>(
                name: "Scopes",
                table: "ClinicApiKeys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: "[\"scan\",\"medical:read\",\"medical:write\",\"certificates\",\"analytics\"]");
        }
    }
}
