namespace PawTrack.AuthorizationTests.Infrastructure;

[CollectionDefinition("Authorization", DisableParallelization = true)]
public sealed class AuthorizationCollection : ICollectionFixture<AuthorizationTestFactory>;
