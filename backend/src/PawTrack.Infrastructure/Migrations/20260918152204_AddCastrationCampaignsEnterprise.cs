using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCastrationCampaignsEnterprise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CastrationCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutingClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    VenueLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Canton = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReservationsOpenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReservationsCloseAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    ReservedCount = table.Column<int>(type: "int", nullable: false),
                    BasePriceCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ConsentVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CastrationCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CastrationAppointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EligibilitySnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsentVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ConsentAcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BasePriceCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    IvaAmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalAmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExecutingVeterinarianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicalOutcome = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PostOperativeInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CastrationAppointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CastrationAppointments_CastrationCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "CastrationCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CastrationAppointments_CampaignId_PetId",
                table: "CastrationAppointments",
                columns: new[] { "CampaignId", "PetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CastrationAppointments_CampaignId_Status_ScheduledAt",
                table: "CastrationAppointments",
                columns: new[] { "CampaignId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CastrationAppointments_OwnerUserId_Status_ScheduledAt",
                table: "CastrationAppointments",
                columns: new[] { "OwnerUserId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CastrationCampaigns_ExecutingClinicId_Status",
                table: "CastrationCampaigns",
                columns: new[] { "ExecutingClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CastrationCampaigns_OrganizerUserId_Status",
                table: "CastrationCampaigns",
                columns: new[] { "OrganizerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CastrationCampaigns_Status_StartsAt_Canton",
                table: "CastrationCampaigns",
                columns: new[] { "Status", "StartsAt", "Canton" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CastrationAppointments");

            migrationBuilder.DropTable(
                name: "CastrationCampaigns");
        }
    }
}
