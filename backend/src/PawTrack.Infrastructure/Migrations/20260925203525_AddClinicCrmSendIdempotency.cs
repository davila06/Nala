using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicCrmSendIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "ClinicClientCommunicationActivities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClientCommunicationActivities_ClinicId_RequestId",
                table: "ClinicClientCommunicationActivities",
                columns: new[] { "ClinicId", "RequestId" },
                unique: true,
                filter: "[RequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicClientCommunicationActivities_ClinicId_RequestId",
                table: "ClinicClientCommunicationActivities");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "ClinicClientCommunicationActivities");
        }
    }
}
