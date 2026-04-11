using FluentAssertions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Auth;
using PawTrack.Infrastructure.Persistence.Configurations;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-47 security regression tests.
///
/// Gap: <c>RefreshTokenConfiguration</c> only declares an explicit index on
/// <c>TokenHash</c> (for O(1) lookup by hash).  The <c>UserId</c> foreign-key
/// column has no explicit index declaration:
///
///   <code>
///   builder.HasIndex(rt => rt.TokenHash).IsUnique();  // ✓ explicit
///   // builder.HasIndex(rt => rt.UserId);             // ✗ missing
///   </code>
///
/// EF Core adds a convention-based index on FK columns, but convention-based
/// indexes:
/// <list type="bullet">
///   <item>Are silently removed if the EF model convention is changed.</item>
///   <item>Do not document <em>why</em> the index exists (security intent).</item>
///   <item>Cannot be given a stable database name without explicit declaration.</item>
/// </list>
///
/// Security impact — two code paths perform <c>UserId</c>-scoped queries:
/// <list type="number">
///   <item>
///     <b>Theft detection</b>: <c>user.RefreshTokens</c> navigation property
///     loads all tokens for the compromised user and revokes them.
///     Without an explicit index, this is a full <c>RefreshTokens</c> table scan.
///   </item>
///   <item>
///     <b>Token rotation</b>: every rotation call loads the user's token list.
///     At scale, round-trip latency climbs linearly with the token table size.
///   </item>
/// </list>
///
/// Fix:
///   Explicitly declare the index in <c>RefreshTokenConfiguration</c>:
///   <code>
///   builder.HasIndex(rt => rt.UserId).HasDatabaseName("IX_RefreshTokens_UserId");
///   </code>
/// </summary>
public sealed class Round47SecurityRegressionTests
{
    [Fact]
    public void RefreshTokenConfiguration_ExplicitlyDeclares_UserIdIndex()
    {
        // Use an empty convention set so that only explicitly-declared indexes
        // are present in the model — if the index only existed via EF FK convention
        // it would not appear here, making the test fail until the config is updated.
        var conventionSet = new ConventionSet();
        var modelBuilder  = new ModelBuilder(conventionSet);

        var entityTypeBuilder = modelBuilder.Entity<RefreshToken>();
        new RefreshTokenConfiguration().Configure(entityTypeBuilder);

        var entityType = modelBuilder.Model.FindEntityType(typeof(RefreshToken));
        entityType.Should().NotBeNull();

        var hasUserIdIndex = entityType!
            .GetIndexes()
            .Any(ix => ix.Properties.Any(p => p.Name == nameof(RefreshToken.UserId)));

        hasUserIdIndex.Should().BeTrue(
            "RefreshToken.UserId must be explicitly indexed in the EF configuration — " +
            "not only by convention — so that theft-detection and token-rotation queries " +
            "remain O(log n) as the RefreshTokens table grows, and so the index " +
            "survives future EF convention changes");
    }

    [Fact]
    public void RefreshTokenConfiguration_UserIdIndex_HasCanonicalDatabaseName()
    {
        var conventionSet = new ConventionSet();
        var modelBuilder  = new ModelBuilder(conventionSet);

        var entityTypeBuilder = modelBuilder.Entity<RefreshToken>();
        new RefreshTokenConfiguration().Configure(entityTypeBuilder);

        var entityType = modelBuilder.Model.FindEntityType(typeof(RefreshToken))!;

        var userIdIndex = entityType
            .GetIndexes()
            .FirstOrDefault(ix => ix.Properties.Any(p => p.Name == nameof(RefreshToken.UserId)));

        userIdIndex.Should().NotBeNull();
        userIdIndex!.GetDatabaseName().Should().Be("IX_RefreshTokens_UserId",
            "the index name must be stable and canonical so that migrations, " +
            "monitoring dashboards, and query-plan tooling can reference it by name");
    }
}
