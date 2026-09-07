using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class GetAvailableProviderServiceSlotsQueryHandlerTests
{
    [Fact]
    public async Task Handle_TwoHourRuleForOneHourService_ReturnsTwoSlots()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var service = ProviderService.Create(
            Guid.NewGuid(), "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1);
        var date = new DateOnly(2026, 9, 7);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        repository.GetActiveAvailabilityRulesAsync(service.Id, Arg.Any<CancellationToken>())
            .Returns([ServiceAvailabilityRule.Create(service.Id, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0))]);
        repository.GetBookingsByServiceRangeAsync(service.Id, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new GetAvailableProviderServiceSlotsQueryHandler(repository);
        var result = await handler.Handle(new GetAvailableProviderServiceSlotsQuery(service.Id, date), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].StartsAt.Should().Be(new DateTimeOffset(2026, 9, 7, 9, 0, 0, TimeSpan.FromHours(-6)));
    }
}