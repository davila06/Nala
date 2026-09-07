using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderServiceDomainTests
{
    [Fact]
    public void Create_SetsPublishedServiceWithBookingDetails()
    {
        var providerId = Guid.NewGuid();

        var service = ProviderService.Create(
            providerId,
            "  Sesion individual  ",
            "  Trabajo de obediencia basica  ",
            ServiceModality.AtProviderLocation,
            60,
            25_000m,
            1);

        service.ServiceProviderId.Should().Be(providerId);
        service.Name.Should().Be("Sesion individual");
        service.Description.Should().Be("Trabajo de obediencia basica");
        service.Modality.Should().Be(ServiceModality.AtProviderLocation);
        service.DurationMinutes.Should().Be(60);
        service.PriceCrc.Should().Be(25_000m);
        service.Capacity.Should().Be(1);
        service.Status.Should().Be(ProviderServiceStatus.Published);
    }
}