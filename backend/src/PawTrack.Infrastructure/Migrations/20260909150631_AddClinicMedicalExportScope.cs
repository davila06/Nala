using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicMedicalExportScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ClinicApiKeys] SET [Scopes] = REPLACE([Scopes], ']', '\"medical:export\"]') WHERE [Scopes] NOT LIKE '%medical:export%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [ClinicApiKeys] SET [Scopes] = REPLACE([Scopes], ',\"medical:export\"]', ']') WHERE [Scopes] LIKE '%medical:export%'");
        }
    }
}
