using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PawTrack.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitlementCatalogAndConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EntitlementConsumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntitlementKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Units = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CycleStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CycleEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ContextType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContextId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitlementConsumptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanEntitlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntitlementKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValueType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BooleanValue = table.Column<bool>(type: "bit", nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ResetPeriod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanEntitlements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntitlementConsumptions_SubjectId_EntitlementKey_CycleStart_CycleEnd",
                table: "EntitlementConsumptions",
                columns: new[] { "SubjectId", "EntitlementKey", "CycleStart", "CycleEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_EntitlementConsumptions_SubjectId_EntitlementKey_IdempotencyKey",
                table: "EntitlementConsumptions",
                columns: new[] { "SubjectId", "EntitlementKey", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanEntitlements_PlanId_EntitlementKey",
                table: "PlanEntitlements",
                columns: new[] { "PlanId", "EntitlementKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanEntitlements_PlanId_IsActive",
                table: "PlanEntitlements",
                columns: new[] { "PlanId", "IsActive" });

            migrationBuilder.Sql("""
                INSERT INTO PlanEntitlements
                    (Id, PlanId, EntitlementKey, ValueType, NumericValue, Unit, ResetPeriod,
                     IsActive, Version, CreatedAt, UpdatedAt)
                SELECT NEWID(), p.Id, v.EntitlementKey, 'Numeric', v.NumericValue, v.Unit, v.ResetPeriod,
                       1, NEWID(), SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM SubscriptionPlans p
                INNER JOIN (VALUES
                    ('UserPlus', 'MaxPets', 3, 'account', NULL),
                    ('UserPlus', 'MedicalRecordsPreviewLimit', 3, 'account', NULL),
                    ('UserPlus', 'MaxScheduleBlocks', 20, 'account', NULL),
                    ('UserPlus', 'MaxActiveLostCases', 3, 'account', NULL),
                    ('UserPlus', 'AiMatchesPerCycle', 10, 'cycle', 'monthly'),
                    ('UserPlus', 'AiCandidatesPerMatch', 15, 'request', NULL),
                    ('UserPlus', 'BroadcastsPerCasePerDay', 5, 'case', 'daily'),
                    ('UserPlus', 'ScanHistoryRetentionDays', 365, 'days', NULL),
                    ('UserPlus', 'MaxGpsCollars', 1, 'account', NULL),
                    ('UserFamilia', 'MaxPets', 25, 'account', NULL),
                    ('UserFamilia', 'MaxFamilyMembers', 5, 'account', NULL),
                    ('UserFamilia', 'MaxActiveVetReminders', 50, 'account', NULL),
                    ('UserFamilia', 'MaxActiveLostCases', 10, 'account', NULL),
                    ('UserFamilia', 'AiMatchesPerCycle', 30, 'cycle', 'monthly'),
                    ('UserFamilia', 'AiCandidatesPerMatch', 35, 'request', NULL),
                    ('UserFamilia', 'BroadcastsPerCasePerDay', 10, 'case', 'daily'),
                    ('UserFamilia', 'ScanHistoryRetentionDays', 3650, 'days', NULL),
                    ('UserFamilia', 'MaxGpsCollars', 5, 'account', NULL),
                    ('ClinicPlus', 'MaxClinicScansPerCycle', 500, 'cycle', 'monthly'),
                    ('ClinicPlus', 'MaxCertificatesPerCycle', 0, 'cycle', 'monthly'),
                    ('ClinicPartner', 'MaxClinicScansPerCycle', 5000, 'cycle', 'monthly'),
                    ('ClinicPartner', 'MaxApiKeys', 10, 'account', NULL),
                    ('ClinicPartner', 'MaxCertificatesPerCycle', 500, 'cycle', 'monthly'),
                    ('ClinicPartner', 'MaxPassportsPerCycle', 250, 'cycle', 'monthly'),
                    ('ClinicPartner', 'ClinicMedicalExportsPerCycle', 20, 'cycle', 'monthly'),
                    ('ClinicPartner', 'MaxAuthorizedVeterinarians', 25, 'account', NULL),
                    ('StorePlus', 'MaxActiveProducts', 100, 'account', NULL),
                    ('StorePlus', 'MaxOrdersPerCycle', 250, 'cycle', 'monthly'),
                    ('StorePartner', 'MaxActiveProducts', 1000, 'account', NULL),
                    ('StorePartner', 'MaxOrdersPerCycle', 2500, 'cycle', 'monthly'),
                    ('StorePartner', 'MaxLocations', 5, 'account', NULL),
                    ('StorePartner', 'MaxAnalyticsExportsPerCycle', 20, 'cycle', 'monthly'),
                    ('StorePartner', 'BulkImportLimit', 1000, 'operation', NULL),
                    ('ShelterPlus', 'MaxActiveAdoptablePets', 500, 'account', NULL),
                    ('ShelterPlus', 'MaxAdoptionFairsPerYear', 24, 'cycle', 'yearly'),
                    ('MuniBasica', 'MaxCapturesPerYear', 500, 'cycle', 'yearly'),
                    ('MuniFull', 'MaxCapturesPerYear', 5000, 'cycle', 'yearly'),
                    ('MuniFull', 'BulkUpdateLimit', 500, 'operation', NULL),
                    ('MuniRedRegional', 'BulkUpdateLimit', 2000, 'operation', NULL),
                    ('MuniRedRegional', 'MaxCapturesPerYear', 30000, 'cycle', 'yearly')
                ) v(Tier, EntitlementKey, NumericValue, Unit, ResetPeriod) ON p.Tier = v.Tier
                WHERE NOT EXISTS (
                    SELECT 1 FROM PlanEntitlements e
                    WHERE e.PlanId = p.Id AND e.EntitlementKey = v.EntitlementKey);

                INSERT INTO PlanEntitlements
                    (Id, PlanId, EntitlementKey, ValueType, BooleanValue,
                     IsActive, Version, CreatedAt, UpdatedAt)
                SELECT NEWID(), p.Id, v.EntitlementKey, 'Boolean', v.BooleanValue,
                       1, NEWID(), SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM SubscriptionPlans p
                INNER JOIN (VALUES
                    ('UserFamilia', 'BroadcastSchedulingEnabled', CAST(1 AS bit)),
                    ('UserPlus', 'SearchGridActivationEnabled', CAST(1 AS bit)),
                    ('UserFamilia', 'SearchGridActivationEnabled', CAST(1 AS bit)),
                    ('ClinicPartner', 'CsvExportEnabled', CAST(1 AS bit)),
                    ('StorePartner', 'CsvExportEnabled', CAST(1 AS bit)),
                    ('MuniRedRegional', 'RegionalDashboardEnabled', CAST(1 AS bit)),
                    ('MuniRedRegional', 'InterCantonTransfersEnabled', CAST(1 AS bit)),
                    ('MuniRedRegional', 'InstitutionalApiEnabled', CAST(1 AS bit))
                ) v(Tier, EntitlementKey, BooleanValue) ON p.Tier = v.Tier
                WHERE NOT EXISTS (
                    SELECT 1 FROM PlanEntitlements e
                    WHERE e.PlanId = p.Id AND e.EntitlementKey = v.EntitlementKey);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EntitlementConsumptions");

            migrationBuilder.DropTable(
                name: "PlanEntitlements");
        }
    }
}
