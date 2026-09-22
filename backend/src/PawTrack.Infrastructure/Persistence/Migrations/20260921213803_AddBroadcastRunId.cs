using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBroadcastRunId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastRunId",
                table: "BroadcastAttempts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_BroadcastAttempts_BroadcastRunId",
                table: "BroadcastAttempts",
                column: "BroadcastRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BroadcastAttempts_BroadcastRunId",
                table: "BroadcastAttempts");

            migrationBuilder.DropColumn(
                name: "BroadcastRunId",
                table: "BroadcastAttempts");
        }
    }
}
