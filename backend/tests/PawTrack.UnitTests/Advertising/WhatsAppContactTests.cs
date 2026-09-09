using PawTrack.Domain.Stores;

namespace PawTrack.UnitTests.Advertising;

public sealed class WhatsAppContactTests
{
    [Fact]
    public void Store_UpdateWhatsAppContact_NormalizesCostaRicaNumberWhenEnabled()
    {
        var store = Store.Create(Guid.NewGuid(), "Tienda", "Productos", "San Jose", 9m, -84m, "store@example.cr");

        store.UpdateWhatsAppContact("+506 8888-1234", true);

        Assert.Equal("50688881234", store.WhatsAppNumber);
        Assert.True(store.IsWhatsAppContactEnabled);
    }
}