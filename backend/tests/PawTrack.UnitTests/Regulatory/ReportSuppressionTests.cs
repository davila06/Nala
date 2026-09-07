using FluentAssertions;
using PawTrack.Application.Regulatory.Services;

namespace PawTrack.UnitTests.Regulatory;

public sealed class ReportSuppressionTests
{
    [Fact]
    public void Suppress_GroupsBelowThreshold_WithoutChangingQualifyingGroups()
    {
        var rows = new[]
        {
            new ReportGroup("San José", "Dog", 4),
            new ReportGroup("Heredia", "Dog", 5),
            new ReportGroup("Alajuela", "Cat", 9),
        };

        var result = ReportSuppressionService.Apply(rows, threshold: 5);

        result.SuppressedRowCount.Should().Be(1);
        result.Groups.Should().ContainSingle(group =>
            group.Key == "San José" && group.Dimension == "Dog" && group.IsSuppressed);
        result.Groups.Should().Contain(group =>
            group.Key == "Heredia" && group.Dimension == "Dog" && group.Count == 5 && !group.IsSuppressed);
    }

    [Fact]
    public void Suppress_RejectsInvalidThreshold()
    {
        var action = () => ReportSuppressionService.Apply([], threshold: 0);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
