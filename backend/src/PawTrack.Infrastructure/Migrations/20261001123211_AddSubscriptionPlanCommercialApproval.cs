using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanCommercialApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommercialApprovalReference",
                table: "SubscriptionPlans",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CommercialApprovedAt",
                table: "SubscriptionPlans",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommercialApprovedByUserId",
                table: "SubscriptionPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_CommercialApprovedByUserId",
                table: "SubscriptionPlans",
                column: "CommercialApprovedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionPlans_Users_CommercialApprovedByUserId",
                table: "SubscriptionPlans",
                column: "CommercialApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionPlans_Users_CommercialApprovedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionPlans_CommercialApprovedByUserId",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CommercialApprovalReference",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CommercialApprovedAt",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "CommercialApprovedByUserId",
                table: "SubscriptionPlans");
        }
    }
}
