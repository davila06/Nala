using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPetSanitaryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Pets",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DistinctiveMarks",
                table: "Pets",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrochipVerificationNotes",
                table: "Pets",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrochipVerificationStatus",
                table: "Pets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotProvided");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MicrochipVerifiedAt",
                table: "Pets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MicrochipVerifiedByClinicId",
                table: "Pets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResidenceCanton",
                table: "Pets",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsibleOwnerId",
                table: "Pets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "Pets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.AddColumn<DateOnly>(
                name: "SterilizedAt",
                table: "Pets",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SterilizedStatus",
                table: "Pets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");

            migrationBuilder.Sql(
                """
                UPDATE Pets
                SET ResponsibleOwnerId = OwnerId,
                    Sex = CASE WHEN Sex = N'' THEN N'Unknown' ELSE Sex END,
                    SterilizedStatus = CASE WHEN SterilizedStatus = N'' THEN N'Unknown' ELSE SterilizedStatus END,
                    MicrochipVerificationStatus = CASE
                        WHEN MicrochipVerificationStatus = N'' AND MicrochipId IS NOT NULL THEN N'Declared'
                        WHEN MicrochipVerificationStatus = N'' THEN N'NotProvided'
                        ELSE MicrochipVerificationStatus
                    END
                """);

            migrationBuilder.CreateTable(
                name: "PetSanitaryIdentityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorClinicId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PreviousValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PetSanitaryIdentityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pets_MicrochipVerificationStatus",
                table: "Pets",
                column: "MicrochipVerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Pets_ResidenceCanton",
                table: "Pets",
                column: "ResidenceCanton");

            migrationBuilder.CreateIndex(
                name: "IX_PetSanitaryIdentityAuditLogs_Action",
                table: "PetSanitaryIdentityAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_PetSanitaryIdentityAuditLogs_ActorClinicId",
                table: "PetSanitaryIdentityAuditLogs",
                column: "ActorClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_PetSanitaryIdentityAuditLogs_ActorUserId",
                table: "PetSanitaryIdentityAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PetSanitaryIdentityAuditLogs_PetId_CreatedAt",
                table: "PetSanitaryIdentityAuditLogs",
                columns: new[] { "PetId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PetSanitaryIdentityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Pets_MicrochipVerificationStatus",
                table: "Pets");

            migrationBuilder.DropIndex(
                name: "IX_Pets_ResidenceCanton",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "DistinctiveMarks",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MicrochipVerificationNotes",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MicrochipVerificationStatus",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MicrochipVerifiedAt",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "MicrochipVerifiedByClinicId",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "ResidenceCanton",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "ResponsibleOwnerId",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "SterilizedAt",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "SterilizedStatus",
                table: "Pets");
        }
    }
}
