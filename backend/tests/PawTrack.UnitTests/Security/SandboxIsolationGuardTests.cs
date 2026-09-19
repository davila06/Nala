using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PawTrack.API.Middleware;

namespace PawTrack.UnitTests.Security;

public sealed class SandboxIsolationGuardTests
{
    [Fact]
    public void Sandbox_WithIsolationFlags_DoesNotThrow()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Features:SandboxDataOnly"] = "true",
            ["Features:ExternalProviderIntegrationsEnabled"] = "false",
            ["Notifications:ExternalDeliveryEnabled"] = "false",
            ["ConnectionStrings:DefaultConnection"] = "Server=sandbox-sql;Database=PawTrackSandbox;",
            ["Azure:Storage:ServiceUri"] = "https://pawtracksandbox.blob.core.windows.net/",
        });

        var act = () => StartupGuards.EnsureSandboxIsolation(configuration, Environment("Sandbox"));

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Server=pawtrack-prod-sql;Database=PawTrack;")]
    [InlineData("Server=sandbox-sql;Database=PawTrackProd;")]
    public void Sandbox_WithProductionDatabase_Throws(string connectionString)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Features:SandboxDataOnly"] = "true",
            ["Features:ExternalProviderIntegrationsEnabled"] = "false",
            ["Notifications:ExternalDeliveryEnabled"] = "false",
            ["ConnectionStrings:DefaultConnection"] = connectionString,
            ["Azure:Storage:ServiceUri"] = "https://pawtracksandbox.blob.core.windows.net/",
        });

        var act = () => StartupGuards.EnsureSandboxIsolation(configuration, Environment("Sandbox"));

        act.Should().Throw<InvalidOperationException>();
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IWebHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }
}
