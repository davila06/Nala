using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWelfareRoutingConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipientType",
                table: "AnimalWelfareReferrals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecipientUserId",
                table: "AnimalWelfareReferrals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoRoutingRequested",
                table: "AnimalWelfareCases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SuggestedDistanceMetres",
                table: "AnimalWelfareCases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedOrganizationUserId",
                table: "AnimalWelfareCases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedRole",
                table: "AnimalWelfareCases",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareReferrals_RecipientUserId_ReferredAt",
                table: "AnimalWelfareReferrals",
                columns: new[] { "RecipientUserId", "ReferredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AnimalWelfareCases_AutoRoutingRequested_Status_CreatedAt",
                table: "AnimalWelfareCases",
                columns: new[] { "AutoRoutingRequested", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AnimalWelfareReferrals_RecipientUserId_ReferredAt",
                table: "AnimalWelfareReferrals");

            migrationBuilder.DropIndex(
                name: "IX_AnimalWelfareCases_AutoRoutingRequested_Status_CreatedAt",
                table: "AnimalWelfareCases");

            migrationBuilder.DropColumn(
                name: "RecipientType",
                table: "AnimalWelfareReferrals");

            migrationBuilder.DropColumn(
                name: "RecipientUserId",
                table: "AnimalWelfareReferrals");

            migrationBuilder.DropColumn(
                name: "AutoRoutingRequested",
                table: "AnimalWelfareCases");

            migrationBuilder.DropColumn(
                name: "SuggestedDistanceMetres",
                table: "AnimalWelfareCases");

            migrationBuilder.DropColumn(
                name: "SuggestedOrganizationUserId",
                table: "AnimalWelfareCases");

            migrationBuilder.DropColumn(
                name: "SuggestedRole",
                table: "AnimalWelfareCases");
        }
    }
}
