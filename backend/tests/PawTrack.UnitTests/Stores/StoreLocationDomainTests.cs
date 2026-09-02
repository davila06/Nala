using FluentAssertions;
using PawTrack.Domain.Stores;

namespace PawTrack.UnitTests.Stores;

public sealed class StoreLocationDomainTests
{
    [Fact]
    public void Create_SetsActiveAndDefaults()
    {
        var storeId = Guid.NewGuid();
        var location = StoreLocation.Create(storeId, "Sucursal Centro", "Av. Central", 9.93m, -84.08m, "8888-8888");

        location.StoreId.Should().Be(storeId);
        location.IsActive.Should().BeTrue();
        location.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_PrimaryLocation_Throws()
    {
        var location = StoreLocation.Create(Guid.NewGuid(), "Matriz", "Centro", 9.9m, -84m, null, isPrimary: true);

        var act = location.Deactivate;

        act.Should().Throw<InvalidOperationException>();
        location.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_NonPrimaryLocation_SetsInactive()
    {
        var location = StoreLocation.Create(Guid.NewGuid(), "Sucursal 2", "Norte", 9.9m, -84m, null);

        location.Deactivate();

        location.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Reactivate_SetsActiveAgain()
    {
        var location = StoreLocation.Create(Guid.NewGuid(), "Sucursal 2", "Norte", 9.9m, -84m, null);
        location.Deactivate();

        location.Reactivate();

        location.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_ChangesFieldsAndSetsUpdatedAt()
    {
        var location = StoreLocation.Create(Guid.NewGuid(), "Sucursal 2", "Norte", 9.9m, -84m, null);

        location.UpdateDetails("Sucursal Norte", "Nueva dirección", 10m, -84.5m, "7777-7777");

        location.Name.Should().Be("Sucursal Norte");
        location.Address.Should().Be("Nueva dirección");
        location.PhoneNumber.Should().Be("7777-7777");
        location.UpdatedAt.Should().NotBeNull();
    }
}
