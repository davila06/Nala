using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Infrastructure;

public sealed class PawTrackWebApplicationFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own isolated InMemory database.
    private readonly string _dbName = $"PawTrackTest_{Guid.NewGuid()}";

    // Dedicated EF Core internal service provider for InMemory only.
    // Avoids the "multiple providers" error that occurs when SqlServer and InMemory
    // extensions are both present in the application DI container.
    private readonly IServiceProvider _efInternalProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Must NOT match any entry in StartupGuards.KnownWeakJwtKeys and must be ≥ 32 chars.
                ["Jwt:Key"] = "integration-tests-only-xK9#mP2$vQ8!nL5@wR3&jY7*",
                ["Jwt:Issuer"] = "pawtrack-tests",
                ["Jwt:Audience"] = "pawtrack-tests",
                ["Jwt:ExpirySeconds"] = "900",
                ["App:BaseUrl"] = "https://localhost:5001",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=PawTrackTest",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all SQL Server DbContext registrations
            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PawTrackDbContext>));
            if (dbContextOptions is not null)
                services.Remove(dbContextOptions);

            var unitOfWork = services.SingleOrDefault(
                d => d.ServiceType == typeof(IUnitOfWork));
            if (unitOfWork is not null)
                services.Remove(unitOfWork);

            // Add InMemory context using a dedicated internal service provider so EF Core
            // never sees both SqlServer and InMemory providers in the same container.
            services.AddDbContext<PawTrackDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                       .UseInternalServiceProvider(_efInternalProvider));

            services.AddScoped<IUnitOfWork>(sp =>
                sp.GetRequiredService<PawTrackDbContext>());
        });

        builder.UseEnvironment("Testing");
    }
}
