namespace PawTrack.API.Middleware;

/// <summary>
/// Validates critical configuration at startup.
/// Prevents running in production with known development defaults.
/// </summary>
public static class StartupGuards
{
    private static readonly HashSet<string> KnownWeakJwtKeys =
    [
        "dev-only-jwt-signing-key-minimum-32-characters-long!",
        "test-secret-key-minimum-256-bits-for-hmac-sha256",
        "your-secret-key",
        "secret",
    ];

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if a weak/default JWT key
    /// is detected outside of the Development environment.
    /// Also enforces a minimum key length of 32 bytes (256-bit HMAC-SHA256 minimum).
    /// </summary>
    public static void EnsureJwtKeyStrength(IConfiguration configuration, IWebHostEnvironment env)
    {
        var key = configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT signing key is not configured.");

        if (key.Length < 32)
            throw new InvalidOperationException(
                $"JWT signing key must be at least 32 characters (256 bits). Current length: {key.Length}.");

        if (!env.IsDevelopment() && KnownWeakJwtKeys.Contains(key))
            throw new InvalidOperationException(
                "A known development JWT signing key was detected in a non-development environment. " +
                "Provide a cryptographically strong key via Key Vault or environment variables.");
    }

    public static void EnsureSandboxIsolation(IConfiguration configuration, IWebHostEnvironment env)
    {
        if (!env.IsEnvironment("Sandbox")) return;

        if (!configuration.GetValue<bool>("Features:SandboxDataOnly") ||
            configuration.GetValue<bool>("Features:ExternalProviderIntegrationsEnabled") ||
            configuration.GetValue<bool>("Notifications:ExternalDeliveryEnabled"))
            throw new InvalidOperationException(
                "Sandbox must enforce synthetic data and disable all external delivery providers.");

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var storageEndpoint = configuration["Azure:Storage:ServiceUri"]
            ?? configuration["Azure:Storage:ConnectionString"]
            ?? string.Empty;
        var resourceConfiguration = $"{connectionString}|{storageEndpoint}";

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(storageEndpoint) ||
            !connectionString.Contains("sandbox", StringComparison.OrdinalIgnoreCase) ||
            !storageEndpoint.Contains("sandbox", StringComparison.OrdinalIgnoreCase) ||
            resourceConfiguration.Contains("pawtrack-prod", StringComparison.OrdinalIgnoreCase) ||
            resourceConfiguration.Contains("pawtrackprod", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Sandbox requires dedicated non-production database and storage resources.");
    }
}
