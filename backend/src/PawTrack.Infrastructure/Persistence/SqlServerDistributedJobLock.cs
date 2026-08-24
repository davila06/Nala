using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Persistence;

/// <summary>
/// SQL Server distributed lock using <c>sp_getapplock</c> / <c>sp_releaseapplock</c>.
/// A single SQL connection is held open for the duration of the lock — the lock is
/// automatically released when the connection closes (process crash / dispose).
/// Safe for Azure SQL which supports application locks in session scope.
/// </summary>
public sealed class SqlServerDistributedJobLock(
    IConfiguration configuration,
    ILogger<SqlServerDistributedJobLock> logger) : IDistributedJobLock
{
    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string jobName,
        TimeSpan holdDuration,
        CancellationToken ct = default)
    {
        var connStr = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            logger.LogWarning("DistributedJobLock: no connection string configured — running without lock for job {Job}", jobName);
            return new NoopLock();
        }

        var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "EXEC sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout";
        cmd.Parameters.AddWithValue("@Resource", $"PawTrack:Job:{jobName}");
        cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
        cmd.Parameters.AddWithValue("@LockOwner", "Session");
        // Timeout 0 = return immediately if lock is not available
        cmd.Parameters.AddWithValue("@LockTimeout", 0);

        var result = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));

        // sp_getapplock return codes: 0/1 = success, -1 = timeout, -2 = cancelled, -3 = deadlock
        if (result < 0)
        {
            await conn.DisposeAsync();
            logger.LogInformation("DistributedJobLock: another instance holds the lock for job {Job} (code={Code})", jobName, result);
            return null;
        }

        logger.LogDebug("DistributedJobLock: acquired lock for job {Job}", jobName);
        return new SqlAppLockHandle(conn, jobName, logger);
    }

    // ── Lock handle ───────────────────────────────────────────────────────────

    private sealed class SqlAppLockHandle(
        SqlConnection connection,
        string jobName,
        ILogger logger) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = "EXEC sp_releaseapplock @Resource, @LockOwner";
                cmd.Parameters.AddWithValue("@Resource", $"PawTrack:Job:{jobName}");
                cmd.Parameters.AddWithValue("@LockOwner", "Session");
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "DistributedJobLock: failed to release lock for job {Job}", jobName);
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }

    private sealed class NoopLock : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
