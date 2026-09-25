using FluentAssertions;
using PawTrack.Application.Clinics.Queries.GetClinicalConsultationTemplates;

namespace PawTrack.UnitTests.Clinics;

public sealed class GetClinicalConsultationTemplatesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEnterpriseDailyUseTemplates()
    {
        var handler = new GetClinicalConsultationTemplatesQueryHandler();

        var result = await handler.Handle(new GetClinicalConsultationTemplatesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Select(template => template.Key).Should().Contain(["checkup", "vaccine", "surgery"]);
        result.Value.Should().OnlyContain(template => !string.IsNullOrWhiteSpace(template.OwnerSummary));
    }
}
