using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PawTrack.API.Services;

namespace PawTrack.UnitTests.Infrastructure;

public sealed class MigrationExecutionPolicyTests
{
    [Fact]
    public void Resolve_DisablesStartupMigrationsInProductionByDefault()
    {
        var configuration = BuildConfiguration();

        var result = MigrationExecutionPolicy.Resolve(configuration, "Production");

        result.ApplyMigrations.Should().BeFalse();
        result.ExitAfterMigration.Should().BeFalse();
    }

    [Fact]
    public void Resolve_EnablesStartupMigrationsForLocalDevelopment()
    {
        var configuration = BuildConfiguration();

        var result = MigrationExecutionPolicy.Resolve(configuration, "Development");

        result.ApplyMigrations.Should().BeTrue();
        result.ExitAfterMigration.Should().BeFalse();
    }

    [Fact]
    public void Resolve_MigrationJobAppliesAndExits()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Database:MigrationOnly"] = "true",
        });

        var result = MigrationExecutionPolicy.Resolve(configuration, "Production");

        result.ApplyMigrations.Should().BeTrue();
        result.ExitAfterMigration.Should().BeTrue();
    }

    private static IConfiguration BuildConfiguration(
        Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
}
