using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdoptableAnimals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Species = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Breed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Size = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AgeCategory = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AgeMonthsApprox = table.Column<int>(type: "int", nullable: true),
                    Story = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Requirements = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MedicalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVaccinated = table.Column<bool>(type: "bit", nullable: false),
                    IsSterilized = table.Column<bool>(type: "bit", nullable: false),
                    IsMicrochipped = table.Column<bool>(type: "bit", nullable: false),
                    OkWithKids = table.Column<bool>(type: "bit", nullable: false),
                    OkWithDogs = table.Column<bool>(type: "bit", nullable: false),
                    OkWithCats = table.Column<bool>(type: "bit", nullable: false),
                    NeedsYard = table.Column<bool>(type: "bit", nullable: false),
                    RefLat = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RefLng = table.Column<decimal>(type: "decimal(9,6)", nullable: false),
                    RefLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AdoptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PhotoUrls = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptableAnimals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdoptionApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdoptablePetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicantNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    ReviewNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AppliedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdoptionFairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VenueLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Lat = table.Column<double>(type: "float", nullable: false),
                    Lng = table.Column<double>(type: "float", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AnimalIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionFairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptableAnimals_OrganizationUserId",
                table: "AdoptableAnimals",
                column: "OrganizationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptableAnimals_Species_Status",
                table: "AdoptableAnimals",
                columns: new[] { "Species", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptableAnimals_Status",
                table: "AdoptableAnimals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionApplications_AdoptablePetId",
                table: "AdoptionApplications",
                column: "AdoptablePetId");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionApplications_ApplicantUserId",
                table: "AdoptionApplications",
                column: "ApplicantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionApplications_ApplicantUserId_AdoptablePetId",
                table: "AdoptionApplications",
                columns: new[] { "ApplicantUserId", "AdoptablePetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionFairs_OrganizationUserId",
                table: "AdoptionFairs",
                column: "OrganizationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionFairs_StartsAt",
                table: "AdoptionFairs",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionFairs_Status",
                table: "AdoptionFairs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdoptableAnimals");

            migrationBuilder.DropTable(
                name: "AdoptionApplications");

            migrationBuilder.DropTable(
                name: "AdoptionFairs");
        }
    }
}
