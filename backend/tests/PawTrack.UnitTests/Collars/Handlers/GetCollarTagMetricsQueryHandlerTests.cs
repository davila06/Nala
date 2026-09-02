using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Queries.GetCollarTagMetrics;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GetCollarTagMetricsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMetricsFromRepository()
    {
        var repo = Substitute.For<ICollarTagRepository>();
        var metrics = new CollarTagMetricsDto(100, 40, 50, 10, 5, 3);
        repo.GetMetricsAsync(Arg.Any<CancellationToken>()).Returns(metrics);
        var sut = new GetCollarTagMetricsQueryHandler(repo);

        var result = await sut.Handle(new GetCollarTagMetricsQuery(), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(metrics);
    }
}
