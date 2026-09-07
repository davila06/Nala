using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ServiceProviderDomainTests
{
    [Fact]
    public void Create_NormalizesProfileAndSetsPendingStatus()
    {
        var ownerUserId = Guid.NewGuid();

        var provider = ServiceProvider.Create(
            ownerUserId,
            "  Escuela Canina CR  ",
            "  Adiestramiento positivo  ",
            ServiceProviderCategory.Trainer,
            "  San Jose  ",
            9.9347m,
            -84.0875m,
            "  HOLA@EJEMPLO.CR  ");

        provider.UserId.Should().Be(ownerUserId);
        provider.Name.Should().Be("Escuela Canina CR");
        provider.Description.Should().Be("Adiestramiento positivo");
        provider.Category.Should().Be(ServiceProviderCategory.Trainer);
        provider.Address.Should().Be("San Jose");
        provider.ContactEmail.Should().Be("hola@ejemplo.cr");
        provider.Status.Should().Be(ServiceProviderStatus.Pending);
        provider.Id.Should().NotBeEmpty();
        provider.RegisteredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}