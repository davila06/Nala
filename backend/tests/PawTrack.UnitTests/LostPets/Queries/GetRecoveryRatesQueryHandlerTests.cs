using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.Queries.GetRecoveryRates;

namespace PawTrack.UnitTests.LostPets.Queries;

public sealed class GetRecoveryRatesQueryHandlerTests
{
    private readonly IRecoveryStatsReadRepository _statsRepository =
        Substitute.For<IRecoveryStatsReadRepository>();

    [Fact]
    public async Task Handle_NoData_ReturnsZeroedMetrics()
    {
        // Arrange
        _statsRepository.GetRecoveryStatsRawAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new RecoveryStatsRawData(
                TotalReports: 0,
                RecoveredDistancesMeters: [],
                RecoveryDurationsHours: []));

        var sut = new GetRecoveryRatesQueryHandler(_statsRepository);

        // Act
        var result = await sut.Handle(
            new GetRecoveryRatesQuery(null, null, null),
            CancellationToken.None);

        // Assert
        result.TotalReports.Should().Be(0);
        result.RecoveredCount.Should().Be(0);
        result.RecoveryRate.Should().Be(0);
        result.MedianRecoveryHours.Should().BeNull();
        result.MedianDistanceMeters.Should().BeNull();
        result.P90DistanceMeters.Should().BeNull();
        result.DataPoints.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithRecoveredData_ComputesMedianAndP90()
    {
        // Arrange
        _statsRepository.GetRecoveryStatsRawAsync(
                "Dog",
                "Labrador",
                "Montes de Oca",
                Arg.Any<CancellationToken>())
            .Returns(new RecoveryStatsRawData(
                TotalReports: 10,
                RecoveredDistancesMeters: [100, 200, 300, 400, 500],
                RecoveryDurationsHours: [1, 2, 3, 4, 5]));

        var sut = new GetRecoveryRatesQueryHandler(_statsRepository);

        // Act
        var result = await sut.Handle(
            new GetRecoveryRatesQuery("Dog", "Labrador", "Montes de Oca"),
            CancellationToken.None);

        // Assert
        result.TotalReports.Should().Be(10);
        result.RecoveredCount.Should().Be(5);
        result.RecoveryRate.Should().Be(0.5);
        result.MedianRecoveryHours.Should().Be(3);
        result.MedianDistanceMeters.Should().Be(300);
        result.P90DistanceMeters.Should().Be(500);
        result.DataPoints.Should().Be(5);
    }
}
