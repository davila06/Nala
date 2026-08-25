using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PawTrack.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core design-time tools (migrations add/update).
/// Configures the same SQL Server options as production, including NetTopologySuite.
/// </summary>
internal sealed class PawTrackDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PawTrackDbContext>
{
    public PawTrackDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=PawTrackDesignTime;Trusted_Connection=True;",
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 0, maxRetryDelay: TimeSpan.Zero, errorNumbersToAdd: null);
                    sqlOptions.UseNetTopologySuite();
                })
            .Options;

        return new PawTrackDbContext(options);
    }
}
