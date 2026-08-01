namespace PawTrack.IntegrationTests.Infrastructure;

/// <summary>
/// Forcing all integration test classes into the same xUnit collection
/// ensures they run sequentially, preventing concurrent WebApplicationFactory
/// startup conflicts when each class creates its own in-process test server.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<PawTrackWebApplicationFactory>
{
    public const string Name = "Integration";
}
