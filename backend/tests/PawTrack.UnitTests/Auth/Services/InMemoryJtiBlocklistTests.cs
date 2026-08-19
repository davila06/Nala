using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Auth;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.UnitTests.Auth.Services;

/// <summary>
/// Tests the IJtiBlocklist contract via DbJtiBlocklist.
/// InMemoryJtiBlocklist was removed — DbJtiBlocklist is the production implementation.
/// </summary>
public sealed class JtiBlocklistContractTests
{
    private static PawTrackDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PawTrackDbContext(options);
    }

    private static IJtiBlocklist CreateSut(PawTrackDbContext db) => new DbJtiBlocklist(db);

    [Fact]
    public async Task IsBlocked_UnknownJti_ReturnsFalse()
    {
        using var db = CreateContext();
        var sut = CreateSut(db);

        var result = await sut.IsBlockedAsync("unknown-jti-xyz", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsBlocked_AfterAdd_ReturnsTrue()
    {
        using var db = CreateContext();
        var sut = CreateSut(db);
        var jti = Guid.NewGuid().ToString();

        await sut.AddAsync(jti, DateTimeOffset.UtcNow.AddMinutes(15), CancellationToken.None);

        var result = await sut.IsBlockedAsync(jti, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsBlocked_ExpiredEntry_ReturnsFalse()
    {
        using var db = CreateContext();
        var sut = CreateSut(db);
        var jti = Guid.NewGuid().ToString();

        // Already expired
        await sut.AddAsync(jti, DateTimeOffset.UtcNow.AddSeconds(-1), CancellationToken.None);

        var result = await sut.IsBlockedAsync(jti, CancellationToken.None);
        result.Should().BeFalse("expired blocklist entries should not be treated as blocked");
    }
}
