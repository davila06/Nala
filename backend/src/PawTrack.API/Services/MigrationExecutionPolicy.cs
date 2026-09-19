namespace PawTrack.API.Services;

public sealed record MigrationExecutionDecision(
    bool ApplyMigrations,
    bool ExitAfterMigration);

public static class MigrationExecutionPolicy
{
    public static MigrationExecutionDecision Resolve(
        IConfiguration configuration,
        string environmentName)
    {
        var migrationOnly = configuration.GetValue<bool>("Database:MigrationOnly");
        if (migrationOnly)
            return new MigrationExecutionDecision(true, true);

        var applyOnStartup = configuration.GetValue<bool?>("Database:ApplyMigrationsOnStartup")
            ?? environmentName is "Development" or "Local";

        return new MigrationExecutionDecision(applyOnStartup, false);
    }
}
