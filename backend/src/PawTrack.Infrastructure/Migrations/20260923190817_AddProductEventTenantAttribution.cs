using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEventTenantAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProductEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantType",
                table: "ProductEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_TenantId_TenantType_OccurredAt",
                table: "ProductEvents",
                columns: new[] { "TenantId", "TenantType", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductEvents_TenantId_TenantType_OccurredAt",
                table: "ProductEvents");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProductEvents");

            migrationBuilder.DropColumn(
                name: "TenantType",
                table: "ProductEvents");
        }
    }
}
