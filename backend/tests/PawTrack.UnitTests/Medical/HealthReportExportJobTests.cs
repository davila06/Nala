using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Medical;

namespace PawTrack.UnitTests.Medical;

public sealed class HealthReportExportJobTests
{
    [Fact]
    public async Task Execute_CompletesQueuedExportBeyondSynchronousLimit()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var export = HealthReportExport.Queue(pet.Id, ownerId, TimeSpan.FromHours(24));
        var repository = Substitute.For<IHealthReportExportRepository>();
        var pets = Substitute.For<IPetRepository>();
        var family = Substitute.For<IFamilyRepository>();
        var subscriptions = Substitute.For<ISubscriptionService>();
        var timeline = Substitute.For<IHealthTimelineReadRepository>();
        var pdf = Substitute.For<IConsolidatedHealthPdfGenerator>();
        var blob = Substitute.For<IBlobStorageService>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        repository.GetExpiredAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HealthReportExport>());
        repository.GetStaleProcessingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<HealthReportExport>());
        repository.GetQueuedAsync(1, Arg.Any<CancellationToken>()).Returns([export]);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        subscriptions.IsFamiliaAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        var items = Enumerable.Range(0, 6001).Select(index => new HealthTimelineItemDto(
            Guid.NewGuid(), "MedicalRecord", new DateOnly(2026, 9, 1), $"Record {index}", "Other",
            null, null, false)).ToList();
        timeline.GetPageAsync(pet.Id, 0, 50_001, Arg.Any<CancellationToken>())
            .Returns(new HealthTimelinePageDto(items, false));
        pdf.GenerateAsync(Arg.Any<ConsolidatedHealthReportData>(), Arg.Any<CancellationToken>())
            .Returns([0x25, 0x50, 0x44, 0x46]);
        blob.UploadAsync("medical-health-exports", Arg.Any<string>(), Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>())
            .Returns("https://storage.invalid/medical-health-exports/report.pdf");
        var job = new HealthReportExportJob(repository, pets, family, subscriptions, timeline, pdf, blob,
            audit, unitOfWork, Substitute.For<ILogger<HealthReportExportJob>>());

        await job.ExecuteAsync(default);

        export.Status.Should().Be(HealthReportExportStatus.Completed);
        export.ItemCount.Should().Be(6001);
        export.IsDownloadable.Should().BeTrue();
        await pdf.Received(1).GenerateAsync(Arg.Is<ConsolidatedHealthReportData>(data => data.Items.Count == 6001), Arg.Any<CancellationToken>());
        await audit.Received(1).AddAsync(Arg.Is<PawTrack.Domain.Audit.AuditLogEntry>(entry =>
            entry.Action == PawTrack.Domain.Audit.AuditAction.MedicalHealthReportCompleted), Arg.Any<CancellationToken>());
    }
}
