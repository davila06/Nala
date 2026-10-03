using FluentAssertions;
using PawTrack.Domain.Stores.Events;
using PawTrack.Infrastructure.Outbox;

namespace PawTrack.UnitTests.Stores;

public sealed class OutboxMessageTypeResolverTests
{
    [Fact]
    public void Resolve_AssemblyQualifiedEventType()
    {
        var type = typeof(StoreOrderLifecycleDomainEvent);

        OutboxMessageTypeResolver.Resolve(type.AssemblyQualifiedName!).Should().Be(type);
    }

    [Fact]
    public void Resolve_LegacyFullNameFromLoadedDomainAssembly()
    {
        var type = typeof(StoreOrderLifecycleDomainEvent);

        OutboxMessageTypeResolver.Resolve(type.FullName!).Should().Be(type);
    }
}
