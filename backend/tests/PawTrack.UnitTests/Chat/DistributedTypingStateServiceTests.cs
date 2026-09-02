using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PawTrack.API.Services;

namespace PawTrack.UnitTests.Chat;

/// <summary>
/// Verifies the typing-indicator guard works via <see cref="IDistributedCache"/>
/// (fixes the scale-out gap: the old <c>ConcurrentDictionary</c>-backed
/// implementation was per-instance and invisible across Container App replicas).
/// Uses the in-memory <see cref="IDistributedCache"/> implementation — same
/// abstraction Redis implements in production, so this exercises the real code path.
/// </summary>
public sealed class DistributedTypingStateServiceTests
{
    private readonly DistributedTypingStateService _sut =
        new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    [Fact]
    public async Task IsOtherPartyTypingAsync_NoOneTyped_ReturnsFalse()
    {
        var result = await _sut.IsOtherPartyTypingAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsOtherPartyTypingAsync_OtherUserTyped_ReturnsTrue()
    {
        var threadId = Guid.NewGuid();
        var me = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        await _sut.SetTypingAsync(threadId, otherUser);

        (await _sut.IsOtherPartyTypingAsync(threadId, me)).Should().BeTrue();
    }

    [Fact]
    public async Task IsOtherPartyTypingAsync_OnlyISelfTyped_ReturnsFalse()
    {
        var threadId = Guid.NewGuid();
        var me = Guid.NewGuid();

        await _sut.SetTypingAsync(threadId, me);

        (await _sut.IsOtherPartyTypingAsync(threadId, me)).Should().BeFalse();
    }

    [Fact]
    public async Task IsOtherPartyTypingAsync_DifferentThread_DoesNotLeak()
    {
        var threadA = Guid.NewGuid();
        var threadB = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var me = Guid.NewGuid();

        await _sut.SetTypingAsync(threadA, otherUser);

        (await _sut.IsOtherPartyTypingAsync(threadB, me)).Should().BeFalse();
    }
}
