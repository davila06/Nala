using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoteUnverifiedElectronicInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;
                UPDATE [ElectronicInvoices]
                SET [Status] = CASE WHEN [SignedXmlUrl] IS NULL THEN 1 ELSE 2 END,
                    [HaciendaResponseStatus] = NULL,
                    [HaciendaResponseXmlUrl] = NULL,
                    [ProcessedByHaciendaAt] = NULL
                WHERE [Status] = 4 AND [HaciendaResponseStatus] = N'ACEPTADO'
                    AND (([SignedXmlUrl] IS NOT NULL AND [HaciendaResponseXmlUrl] = [SignedXmlUrl])
                        OR ([SignedXmlUrl] IS NULL AND [HaciendaResponseXmlUrl] = N''));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
