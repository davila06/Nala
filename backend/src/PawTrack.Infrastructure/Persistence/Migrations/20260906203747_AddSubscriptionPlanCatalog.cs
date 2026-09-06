using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlanCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MonthlyPriceCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    AnnualPriceCrc = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO SubscriptionPlans
                    (Id, Tier, DisplayName, Description, MonthlyPriceCrc, AnnualPriceCrc,
                     IsActive, CreatedAt, UpdatedAt, Version)
                VALUES
                    (NEWID(), 'UserPlus', 'Plus', 'Plan avanzado para dueños de mascotas', 2990, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'UserFamilia', 'Familia', 'Plan familiar para múltiples miembros y mascotas', 4990, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'ClinicPlus', 'Clínica Plus', 'Herramientas operativas y visibilidad para clínicas', 15000, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'ClinicPartner', 'Clínica Partner', 'Integraciones y capacidades avanzadas para clínicas', 35000, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'StorePlus', 'Tienda Plus', 'Catálogo y pedidos in-app para tiendas', 12000, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'StorePartner', 'Tienda Partner', 'Analytics y capacidades avanzadas para tiendas', 25000, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'ShelterPlus', 'Refugio Plus', 'Adopciones ilimitadas y ferias geolocalizadas', 8000, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'MuniBasica', 'Municipal Básica', 'Portal municipal básico para un cantón', NULL, 150000, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'MuniFull', 'Municipal Full', 'Portal municipal con fotos, estadísticas y API', NULL, 300000, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID()),
                    (NEWID(), 'MuniRedRegional', 'Red Regional', 'Dashboard regional multi-cantón', NULL, 500000, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), NEWID());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
