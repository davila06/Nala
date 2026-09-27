using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Medical;

public sealed class ConsolidatedHealthReportQueryTests
{
    [Fact]
    public async Task OwnerWithFamilia_ExportsAllPagesIncludingCertificatesAndDeclaredDocuments()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var pets = Substitute.For<IPetRepository>();
        var family = Substitute.For<IFamilyRepository>();
        var plans = Substitute.For<ISubscriptionService>();
        var timeline = Substitute.For<IHealthTimelineReadRepository>();
        var renderer = Substitute.For<IConsolidatedHealthPdfGenerator>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        plans.IsFamiliaAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        timeline.GetPageAsync(pet.Id, 0, 100, Arg.Any<CancellationToken>()).Returns(
            new HealthTimelinePageDto([new HealthTimelineItemDto(Guid.NewGuid(), "MedicalRecord",
                new DateOnly(2026, 9, 20), "Examen", "Other", "https://example.invalid/exam.pdf", null, false, "Radiograph")], true));
        timeline.GetPageAsync(pet.Id, 100, 100, Arg.Any<CancellationToken>()).Returns(
            new HealthTimelinePageDto([new HealthTimelineItemDto(Guid.NewGuid(), "Certificate",
                new DateOnly(2026, 9, 19), "HealthClearance", "Certificate", null, "SAFE-1", false)], false));
        renderer.GenerateAsync(Arg.Any<ConsolidatedHealthReportData>(), Arg.Any<CancellationToken>())
            .Returns([0x25, 0x50, 0x44, 0x46]);

        var handler = new GenerateConsolidatedHealthReportQueryHandler(pets, family, plans, timeline, renderer);
        var result = await handler.Handle(new GenerateConsolidatedHealthReportQuery(pet.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        await renderer.Received(1).GenerateAsync(
            Arg.Is<ConsolidatedHealthReportData>(data => data.Items.Count == 2 &&
                data.Items[0].DocumentKind == "Radiograph" && data.Items[1].VerificationCode == "SAFE-1"),
            Arg.Any<CancellationToken>());
    }
}
