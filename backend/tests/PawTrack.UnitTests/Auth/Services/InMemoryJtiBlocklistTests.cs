using FluentAssertions;
using PawTrack.Infrastructure.Auth;

namespace PawTrack.UnitTests.Auth.Services;

public sealed class InMemoryJtiBlocklistTests
{
    private readonly InMemoryJtiBlocklist _sut = new();

    [Fact]
    public async Task IsBlocked_UnknownJti_ReturnsFalse()
    {
        var result = await _sut.IsBlockedAsync("unknown-jti-xyz", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsBlocked_AfterAdd_ReturnsTrue()
    {
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        await _sut.AddAsync(jti, expiresAt, CancellationToken.None);

        var result = await _sut.IsBlockedAsync(jti, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsBlocked_ExpiredEntry_ReturnsFalse()
    {
        var jti = Guid.NewGuid().ToString();
        // Already expired
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);

        await _sut.AddAsync(jti, expiresAt, CancellationToken.None);

        var result = await _sut.IsBlockedAsync(jti, CancellationToken.None);
        result.Should().BeFalse("expired blocklist entries should not be treated as blocked");
    }
}
