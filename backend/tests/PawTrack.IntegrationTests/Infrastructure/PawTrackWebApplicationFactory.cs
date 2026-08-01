using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Infrastructure;

public sealed class PawTrackWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"PawTrackTest_{Guid.NewGuid()}";

    private readonly IServiceProvider _efInternalProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

    /// <summary>Stores the last raw verification token sent by the email sender stub.</summary>
    public string? LastVerificationToken => _emailSender.LastVerificationToken;

    private readonly CapturingEmailSender _emailSender = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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
            // ── Replace SQL Server DbContext with InMemory ──────────────────
            var dbContextOptions = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PawTrackDbContext>));
            if (dbContextOptions is not null) services.Remove(dbContextOptions);

            var unitOfWork = services.SingleOrDefault(d => d.ServiceType == typeof(IUnitOfWork));
            if (unitOfWork is not null) services.Remove(unitOfWork);

            services.AddDbContext<PawTrackDbContext>(options =>
                options.UseInMemoryDatabase(_dbName)
                       .UseInternalServiceProvider(_efInternalProvider));

            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PawTrackDbContext>());

            // ── Stub IEmailSender — captures verification token ─────────────
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
            if (emailDescriptor is not null) services.Remove(emailDescriptor);
            services.AddSingleton<IEmailSender>(_emailSender);

            // ── Stub ICertificateService (PDF → blob) ───────────────────────
            var certDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICertificateService));
            if (certDescriptor is not null) services.Remove(certDescriptor);
            services.AddSingleton<ICertificateService, StubCertificateService>();

            // ── Stub IBlobStorageService ─────────────────────────────────────
            var blobDescriptors = services.Where(d => d.ServiceType == typeof(IBlobStorageService)).ToList();
            foreach (var d in blobDescriptors) services.Remove(d);
            services.AddSingleton<IBlobStorageService, StubBlobStorageService>();

            // ── Override JwtBearerOptions to use the test key at VALIDATION time ──
            // Program.cs captures jwtKey at startup (before ConfigureWebHost runs),
            // so validation would use appsettings.Testing.json key while generation
            // uses the factory's key. This post-configuration aligns them.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var cfg = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var testKey = cfg["Jwt:Key"];
                if (testKey is not null)
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testKey));
                    options.TokenValidationParameters.ValidIssuer = cfg["Jwt:Issuer"];
                    options.TokenValidationParameters.ValidAudience = cfg["Jwt:Audience"];
                }
            });
        });

        builder.UseEnvironment("Testing");
    }
}
