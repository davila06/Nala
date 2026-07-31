using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsBountiesCollars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bounties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LostPetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepositReference = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    PlatformFee = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    ClaimedBySightingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DepositedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bounties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollarLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lat = table.Column<double>(type: "float", nullable: false),
                    Lng = table.Column<double>(type: "float", nullable: false),
                    Accuracy = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Collars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalDeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalTokenEncrypted = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BatteryPercent = table.Column<int>(type: "int", nullable: true),
                    LastLat = table.Column<double>(type: "float", nullable: true),
                    LastLng = table.Column<double>(type: "float", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    AmountCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bounties_DepositReference",
                table: "Bounties",
                column: "DepositReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bounties_LostPetEventId",
                table: "Bounties",
                column: "LostPetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_Bounties_LostPetEventId_Status",
                table: "Bounties",
                columns: new[] { "LostPetEventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CollarLocations_CollarId_RecordedAt",
                table: "CollarLocations",
                columns: new[] { "CollarId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Collars_PetId_IsActive",
                table: "Collars",
                columns: new[] { "PetId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ClinicId",
                table: "Subscriptions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ClinicId_Status",
                table: "Subscriptions",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaymentReference",
                table: "Subscriptions",
                column: "PaymentReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId_Status",
                table: "Subscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bounties");

            migrationBuilder.DropTable(
                name: "CollarLocations");

            migrationBuilder.DropTable(
                name: "Collars");

            migrationBuilder.DropTable(
                name: "Subscriptions");
        }
    }
}
