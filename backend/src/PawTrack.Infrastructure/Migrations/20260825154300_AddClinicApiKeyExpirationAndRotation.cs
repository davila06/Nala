using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicApiKeyExpirationAndRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server column DEFAULT constraints cannot reference another column
            // (e.g. DATEADD(year, 1, [CreatedAt])) — add nullable, backfill, then tighten.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "ClinicApiKeys",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [ClinicApiKeys] SET [ExpiresAt] = DATEADD(year, 1, [CreatedAt]) WHERE [ExpiresAt] IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "ClinicApiKeys",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RotatedToKeyId",
                table: "ClinicApiKeys",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ClinicApiKeys");

            migrationBuilder.DropColumn(
                name: "RotatedToKeyId",
                table: "ClinicApiKeys");
        }
    }
}
