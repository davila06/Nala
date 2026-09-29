using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Persistence;
using System.Text;

namespace PawTrack.AuthorizationTests.Infrastructure;

public sealed class AuthorizationTestFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"PawTrackAuthorizationTests_{Guid.NewGuid():N}";
    private readonly ServiceProvider efProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "authorization-tests-only-key-minimum-256-bits-for-hmac",
                ["Jwt:Issuer"] = "pawtrack-authorization-tests",
                ["Jwt:Audience"] = "pawtrack-authorization-tests",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Database:MigrationOnly"] = "false",
                ["RateLimiting:PublicApi:PermitLimit"] = "1000",
                ["RateLimiting:PublicApi:WindowSeconds"] = "3600",
            });
        });

        builder.ConfigureServices(services =>
        {
            var options = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions<PawTrackDbContext>) ||
                    descriptor.ServiceType == typeof(IDbContextOptionsConfiguration<PawTrackDbContext>))
                .ToList();
            foreach (var descriptor in options) services.Remove(descriptor);

            services.AddDbContext<PawTrackDbContext>(dbOptions =>
                dbOptions.UseInMemoryDatabase(databaseName)
                    .UseInternalServiceProvider(efProvider));

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService, StubBlobStorageService>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes("authorization-tests-only-key-minimum-256-bits-for-hmac"));
                options.TokenValidationParameters.ValidIssuer = "pawtrack-authorization-tests";
                options.TokenValidationParameters.ValidAudience = "pawtrack-authorization-tests";
            });
        });
    }
}
