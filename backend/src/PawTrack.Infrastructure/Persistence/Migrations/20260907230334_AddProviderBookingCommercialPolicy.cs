using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderBookingCommercialPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF COL_LENGTH('ProviderBookings', 'CustomerRefundPercentage') IS NULL ALTER TABLE [ProviderBookings] ADD [CustomerRefundPercentage] decimal(5,2) NOT NULL CONSTRAINT [DF_ProviderBookings_CustomerRefundPercentage] DEFAULT 0.0;");
            migrationBuilder.Sql("IF COL_LENGTH('ProviderBookings', 'FreeCancellationHours') IS NULL ALTER TABLE [ProviderBookings] ADD [FreeCancellationHours] int NOT NULL CONSTRAINT [DF_ProviderBookings_FreeCancellationHours] DEFAULT 0;");
            migrationBuilder.Sql("IF COL_LENGTH('ProviderBookings', 'ProviderCancellationRefundPercentage') IS NULL ALTER TABLE [ProviderBookings] ADD [ProviderCancellationRefundPercentage] decimal(5,2) NOT NULL CONSTRAINT [DF_ProviderBookings_ProviderCancellationRefundPercentage] DEFAULT 0.0;");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerRefundPercentage",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "FreeCancellationHours",
                table: "ProviderBookings");

            migrationBuilder.DropColumn(
                name: "ProviderCancellationRefundPercentage",
                table: "ProviderBookings");
        }
    }
}
