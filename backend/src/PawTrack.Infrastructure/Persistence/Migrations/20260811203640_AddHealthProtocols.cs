using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthProtocols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthProtocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Species = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordType = table.Column<int>(type: "int", nullable: false),
                    ProtocolName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthProtocols", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HealthProtocols",
                columns: new[] { "Id", "IntervalDays", "ProtocolName", "RecordType", "Species" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), 365, "Vacunación anual", 0, "Dog" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), 180, "Desparasitación semestral", 1, "Dog" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), 365, "Revisión veterinaria anual", 2, "Dog" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), 365, "Vacunación anual", 0, "Cat" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), 180, "Desparasitación semestral", 1, "Cat" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), 365, "Revisión veterinaria anual", 2, "Cat" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), 90, "Desparasitación trimestral", 1, "Rabbit" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), 180, "Revisión veterinaria semestral", 2, "Rabbit" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), 365, "Revisión veterinaria anual", 2, "Bird" },
                    { new Guid("10000000-0000-0000-0000-00000000000a"), 365, "Vacunación anual", 0, "Other" },
                    { new Guid("10000000-0000-0000-0000-00000000000b"), 180, "Desparasitación semestral", 1, "Other" },
                    { new Guid("10000000-0000-0000-0000-00000000000c"), 365, "Revisión veterinaria anual", 2, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthProtocols_Species_RecordType",
                table: "HealthProtocols",
                columns: new[] { "Species", "RecordType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthProtocols");
        }
    }
}
