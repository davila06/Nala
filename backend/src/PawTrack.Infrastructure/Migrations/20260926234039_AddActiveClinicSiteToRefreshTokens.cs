using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddActiveClinicSiteToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveClinicId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ActiveClinicId",
                table: "RefreshTokens",
                column: "ActiveClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Clinics_ActiveClinicId",
                table: "RefreshTokens",
                column: "ActiveClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Clinics_ActiveClinicId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ActiveClinicId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ActiveClinicId",
                table: "RefreshTokens");
        }
    }
}
