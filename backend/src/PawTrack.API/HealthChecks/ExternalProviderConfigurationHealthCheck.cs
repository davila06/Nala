using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PawTrack.API.HealthChecks;

public sealed class ExternalProviderConfigurationHealthCheck(
    IConfiguration configuration,
    IHostEnvironment environment) : IHealthCheck
{
    private static readonly (string Provider, string Key)[] RequiredSettings =
    [
        ("Azure Storage", "Azure:Storage:ConnectionString"),
        ("Application Insights", "ApplicationInsights:ConnectionString"),
        ("Email", "SendGrid:ApiKey"),
        ("WhatsApp", "Broadcast:WhatsApp:AccessToken"),
        ("Telegram", "Broadcast:Telegram:BotToken"),
        ("Facebook", "Broadcast:Facebook:PageAccessToken"),
    ];

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing") || environment.IsEnvironment("Local"))
            return Task.FromResult(HealthCheckResult.Healthy("External provider configuration is not required locally."));

        var missing = RequiredSettings
            .Where(setting => string.IsNullOrWhiteSpace(configuration[setting.Key]))
            .Select(setting => setting.Provider)
            .ToArray();

        if (missing.Length == 0)
            return Task.FromResult(HealthCheckResult.Healthy("Required external provider configuration is present."));

        var data = new Dictionary<string, object>
        {
            ["missingProviders"] = string.Join(", ", missing),
        };
        return Task.FromResult(HealthCheckResult.Unhealthy(
            "Required external provider configuration is missing.",
            data: data));
    }
}
