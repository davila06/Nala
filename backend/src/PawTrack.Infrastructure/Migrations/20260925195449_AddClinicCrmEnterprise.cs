using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicCrmEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicClientCommunicationActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicClientCommunicationActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicClientCommunicationPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsOptedIn = table.Column<bool>(type: "bit", nullable: false),
                    ConsentSource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicClientCommunicationPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicCrmTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicCrmTasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClientCommunicationActivities_ClinicId_CreatedAt",
                table: "ClinicClientCommunicationActivities",
                columns: new[] { "ClinicId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClientCommunicationActivities_ClinicId_PetId",
                table: "ClinicClientCommunicationActivities",
                columns: new[] { "ClinicId", "PetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClientCommunicationPreferences_ClinicId_OwnerUserId",
                table: "ClinicClientCommunicationPreferences",
                columns: new[] { "ClinicId", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicClientCommunicationPreferences_ClinicId_PetId_Channel_Purpose",
                table: "ClinicClientCommunicationPreferences",
                columns: new[] { "ClinicId", "PetId", "Channel", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_PetId",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "PetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_Status_DueDate",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicClientCommunicationActivities");

            migrationBuilder.DropTable(
                name: "ClinicClientCommunicationPreferences");

            migrationBuilder.DropTable(
                name: "ClinicCrmTasks");
        }
    }
}
