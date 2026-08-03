using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesAndEFConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VetReminders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "VetReminders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VetName",
                table: "MedicalRecords",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrl",
                table: "MedicalRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MedicalRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ClinicName",
                table: "MedicalRecords",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvitedEmail",
                table: "FamilyInvitations",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FamilyAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_VetReminders_DueDate_IsCompleted",
                table: "VetReminders",
                columns: new[] { "DueDate", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_VetReminders_PetId",
                table: "VetReminders",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_VetReminders_PetId_IsCompleted",
                table: "VetReminders",
                columns: new[] { "PetId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_ClinicId",
                table: "MedicalRecords",
                column: "ClinicId",
                filter: "[ClinicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PetId",
                table: "MedicalRecords",
                column: "PetId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalRecords_PetId_Date",
                table: "MedicalRecords",
                columns: new[] { "PetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberships_FamilyAccountId_UserId",
                table: "FamilyMemberships",
                columns: new[] { "FamilyAccountId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyMemberships_UserId",
                table: "FamilyMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyInvitations_InvitedEmail",
                table: "FamilyInvitations",
                column: "InvitedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyInvitations_Token",
                table: "FamilyInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FamilyAccounts_OwnerId",
                table: "FamilyAccounts",
                column: "OwnerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicScans_MatchedPetId",
                table: "ClinicScans",
                column: "MatchedPetId",
                filter: "[MatchedPetId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VetReminders_DueDate_IsCompleted",
                table: "VetReminders");

            migrationBuilder.DropIndex(
                name: "IX_VetReminders_PetId",
                table: "VetReminders");

            migrationBuilder.DropIndex(
                name: "IX_VetReminders_PetId_IsCompleted",
                table: "VetReminders");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_ClinicId",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PetId",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_MedicalRecords_PetId_Date",
                table: "MedicalRecords");

            migrationBuilder.DropIndex(
                name: "IX_FamilyMemberships_FamilyAccountId_UserId",
                table: "FamilyMemberships");

            migrationBuilder.DropIndex(
                name: "IX_FamilyMemberships_UserId",
                table: "FamilyMemberships");

            migrationBuilder.DropIndex(
                name: "IX_FamilyInvitations_InvitedEmail",
                table: "FamilyInvitations");

            migrationBuilder.DropIndex(
                name: "IX_FamilyInvitations_Token",
                table: "FamilyInvitations");

            migrationBuilder.DropIndex(
                name: "IX_FamilyAccounts_OwnerId",
                table: "FamilyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_ClinicScans_MatchedPetId",
                table: "ClinicScans");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "VetReminders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "VetReminders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VetName",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DocumentUrl",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ClinicName",
                table: "MedicalRecords",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvitedEmail",
                table: "FamilyInvitations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "FamilyAccounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
