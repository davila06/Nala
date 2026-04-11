using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PawTrack.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so that <c>dotnet ef migrations add</c> can construct
/// <see cref="PawTrackDbContext"/> without requiring the full application host.
/// </summary>
internal sealed class PawTrackDbContextFactory : IDesignTimeDbContextFactory<PawTrackDbContext>
{
    public PawTrackDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PawTrackDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=PawTrackDev;Trusted_Connection=True;",
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null));

        return new PawTrackDbContext(optionsBuilder.Options);
    }
}
