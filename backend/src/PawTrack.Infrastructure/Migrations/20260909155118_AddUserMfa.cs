using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MfaConfiguredAt",
                table: "Users",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecretProtected",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Scopes",
                table: "ClinicApiKeys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "[\"scan\",\"medical:read\",\"medical:write\",\"medical:export\",\"certificates\",\"analytics\"]",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldDefaultValue: "[\"scan\",\"medical:read\",\"medical:write\",\"certificates\",\"analytics\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MfaConfiguredAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaSecretProtected",
                table: "Users");

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
                oldDefaultValue: "[\"scan\",\"medical:read\",\"medical:write\",\"medical:export\",\"certificates\",\"analytics\"]");
        }
    }
}
