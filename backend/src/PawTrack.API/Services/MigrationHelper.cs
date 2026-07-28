using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PawTrack.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace PawTrack.API.Services;

/// <summary>
/// Handles EF Core migration execution at startup with safe handling for
/// databases previously created by <c>EnsureCreated()</c>.
/// <para>
/// The "fake-apply" logic solves the bootstrap problem:
/// When a database was created with <c>EnsureCreated</c> (all tables exist but
/// no <c>__EFMigrationsHistory</c>), running <c>Migrate()</c> directly would
/// fail because <c>InitialCreate</c> tries to CREATE TABLE on existing tables.
/// This helper detects that state and inserts all migration IDs as applied
/// without executing their SQL, establishing the baseline.
/// Subsequent schema changes are applied normally via new migrations.
/// </para>
/// </summary>
public static class MigrationHelper
{
    private const string MigrationsTable = "__EFMigrationsHistory";

    private const string CreateHistoryTableSql =
        "IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__EFMigrationsHistory') " +
        "CREATE TABLE [__EFMigrationsHistory] " +
        "([MigrationId] nvarchar(150) NOT NULL, " +
        "[ProductVersion] nvarchar(32) NOT NULL, " +
        "CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]))";

    /// <summary>
    /// Applies pending EF Core migrations to the database.
    /// Safe to call on every startup — idempotent.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();

        try
        {
            // ── Step 1: ensure the DB server is reachable ─────────────────────
            await db.Database.OpenConnectionAsync(cancellationToken);
            await db.Database.CloseConnectionAsync();

            // ── Step 2: detect EnsureCreated-bootstrapped database ────────────
            // A DB created with EnsureCreated has all the tables but no
            // __EFMigrationsHistory table. We must fake-apply the baseline
            // before calling Migrate(), otherwise InitialCreate fails trying
            // to CREATE TABLE on tables that already exist.
            var historyExists = await CheckHistoryTableExistsAsync(db, cancellationToken);

            if (!historyExists)
            {
                logger.LogInformation(
                    "[Migrations] No __EFMigrationsHistory found. " +
                    "Checking whether schema was bootstrapped via EnsureCreated…");

                var usersTableExists = await CheckUsersTableExistsAsync(db, cancellationToken);

                if (usersTableExists)
                {
                    // Existing EnsureCreated database — insert all migrations as applied
                    await FakeApplyAllMigrationsAsync(db, logger, cancellationToken);
                    logger.LogInformation("[Migrations] Baseline established. Future schema changes will use migrations.");
                    return;
                }

                // Completely fresh database — run all migrations normally
                logger.LogInformation("[Migrations] Fresh database detected. Running all migrations…");
            }

            // ── Step 3: apply any pending migrations ──────────────────────────
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("[Migrations] Database is up to date. No migrations to apply.");
                return;
            }

            logger.LogInformation("[Migrations] Applying {Count} pending migration(s): {Names}",
                pending.Count, string.Join(", ", pending));

            await db.Database.MigrateAsync(cancellationToken);

            logger.LogInformation("[Migrations] All migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Migrations] Failed to apply migrations. The application may not function correctly.");
            throw; // Fail fast — a broken schema means a broken app
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<bool> CheckHistoryTableExistsAsync(
        PawTrackDbContext db, CancellationToken ct)
    {
        return await ExecuteScalarAsync(db,
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
            "WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '__EFMigrationsHistory'", ct) > 0;
    }

    private static async Task<bool> CheckUsersTableExistsAsync(
        PawTrackDbContext db, CancellationToken ct)
    {
        return await ExecuteScalarAsync(db,
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
            "WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Users'", ct) > 0;
    }

    /// <summary>Executes a scalar SQL query and returns the integer result using raw ADO.NET.</summary>
    private static async Task<int> ExecuteScalarAsync(
        PawTrackDbContext db, string sql, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var wasOpen = conn.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync(ct);
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            var result = await cmd.ExecuteScalarAsync(ct);
            return result is int i ? i : Convert.ToInt32(result);
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    private static async Task FakeApplyAllMigrationsAsync(
        PawTrackDbContext db, ILogger logger, CancellationToken ct)
    {
        // Create the history table first
        await db.Database.ExecuteSqlRawAsync(CreateHistoryTableSql, ct);

        // Resolve the EF runtime version embedded in the current assembly
        var efVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "9.0.3";

        // Get all migrations defined in the assembly (ordered)
        var allMigrations = db.Database.GetMigrations().ToList();

        logger.LogInformation(
            "[Migrations] Fake-applying {Count} baseline migrations as already applied…",
            allMigrations.Count);

        foreach (var migrationId in allMigrations)
        {
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1})",
                migrationId, efVersion);
        }

        logger.LogInformation("[Migrations] Baseline: {Count} migrations marked as applied.", allMigrations.Count);
    }
}
