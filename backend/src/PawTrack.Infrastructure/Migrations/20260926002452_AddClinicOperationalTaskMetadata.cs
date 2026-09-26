using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicOperationalTaskMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicCrmTasks_ClinicId_Status_DueDate",
                table: "ClinicCrmTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "PetId",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "AssignedRole",
                table: "ClinicCrmTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "ClinicCrmTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE dbo.ClinicCrmTasks
                SET AssignedRole = CASE Type
                    WHEN N'CallClient' THEN N'Receptionist'
                    WHEN N'ConfirmAppointment' THEN N'Receptionist'
                    WHEN N'FollowUpTreatment' THEN N'Veterinarian'
                    WHEN N'SendDocument' THEN N'Veterinarian'
                    WHEN N'Reactivation' THEN N'Veterinarian'
                    WHEN N'PrepareConsultation' THEN N'Assistant'
                    WHEN N'ReviewInventory' THEN N'Assistant'
                    WHEN N'CollectPayment' THEN N'Cashier'
                    ELSE N'Manager'
                END,
                Priority = N'Normal',
                IdempotencyKey = NEWID();
                """);

            migrationBuilder.AlterColumn<string>(
                name: "AssignedRole",
                table: "ClinicCrmTasks",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "IdempotencyKey",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Priority",
                table: "ClinicCrmTasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_AssignedRole_Status_Priority_DueDate",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "AssignedRole", "Status", "Priority", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_AssignedToUserId_Status",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "AssignedToUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_IdempotencyKey",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM dbo.ClinicCrmTasks WHERE PetId IS NULL OR OwnerUserId IS NULL;");

            migrationBuilder.DropIndex(
                name: "IX_ClinicCrmTasks_ClinicId_AssignedRole_Status_Priority_DueDate",
                table: "ClinicCrmTasks");

            migrationBuilder.DropIndex(
                name: "IX_ClinicCrmTasks_ClinicId_AssignedToUserId_Status",
                table: "ClinicCrmTasks");

            migrationBuilder.DropIndex(
                name: "IX_ClinicCrmTasks_ClinicId_IdempotencyKey",
                table: "ClinicCrmTasks");

            migrationBuilder.DropColumn(
                name: "AssignedRole",
                table: "ClinicCrmTasks");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "ClinicCrmTasks");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "ClinicCrmTasks");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ClinicCrmTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "PetId",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerUserId",
                table: "ClinicCrmTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicCrmTasks_ClinicId_Status_DueDate",
                table: "ClinicCrmTasks",
                columns: new[] { "ClinicId", "Status", "DueDate" });
        }
    }
}
