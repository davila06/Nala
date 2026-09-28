using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Medical;

public sealed class HealthReportExportCommandTests
{
    [Fact]
    public async Task Download_AnotherRequesterCannotReadBlob()
    {
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var petId = Guid.NewGuid();
        var export = HealthReportExport.Queue(petId, ownerId, TimeSpan.FromHours(24));
        export.Start(DateTimeOffset.UtcNow).Should().BeTrue();
        export.Complete("https://storage.invalid/medical-health-exports/owner/pet/report.pdf",
            6001, DateTimeOffset.UtcNow).Should().BeTrue();
        var repository = Substitute.For<IHealthReportExportRepository>();
        var blob = Substitute.For<IBlobStorageService>();
        repository.GetByIdAsync(export.Id, Arg.Any<CancellationToken>()).Returns(export);
        var handler = new DownloadHealthReportExportQueryHandler(repository,
            Substitute.For<IPetRepository>(), Substitute.For<IFamilyRepository>(),
            Substitute.For<ISubscriptionService>(), blob, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new DownloadHealthReportExportQuery(export.Id, strangerId), default);

        result.IsFailure.Should().BeTrue();
        await blob.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Request_ReusesExistingActiveExportForSameOwnerAndPet()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var existing = HealthReportExport.Queue(pet.Id, ownerId, TimeSpan.FromHours(24));
        var pets = Substitute.For<IPetRepository>();
        var plans = Substitute.For<ISubscriptionService>();
        var exports = Substitute.For<IHealthReportExportRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        plans.IsFamiliaAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        exports.GetReusableAsync(ownerId, pet.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new RequestHealthReportExportCommandHandler(pets, Substitute.For<IFamilyRepository>(),
            plans, exports, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new RequestHealthReportExportCommand(pet.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        await exports.DidNotReceive().AddAsync(Arg.Any<HealthReportExport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Request_ConcurrentInsertConflict_ReturnsWinningActiveExport()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var winner = HealthReportExport.Queue(pet.Id, ownerId, TimeSpan.FromHours(24));
        var pets = Substitute.For<IPetRepository>();
        var plans = Substitute.For<ISubscriptionService>();
        var exports = Substitute.For<IHealthReportExportRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        plans.IsFamiliaAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        exports.GetReusableAsync(ownerId, pet.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<HealthReportExport?>(null),
                _ => Task.FromResult<HealthReportExport?>(winner));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new InvalidOperationException("unique active export conflict")));
        var handler = new RequestHealthReportExportCommandHandler(pets, Substitute.For<IFamilyRepository>(),
            plans, exports, Substitute.For<IAuditLogRepository>(), unitOfWork);

        var result = await handler.Handle(new RequestHealthReportExportCommand(pet.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(winner.Id);
        await exports.Received(2).GetReusableAsync(ownerId, pet.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Request_OtherOwner_IsDeniedBeforeEnqueue()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);
        var pets = Substitute.For<IPetRepository>();
        var plans = Substitute.For<ISubscriptionService>();
        var exports = Substitute.For<IHealthReportExportRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        plans.IsFamiliaAsync(outsiderId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RequestHealthReportExportCommandHandler(pets, Substitute.For<IFamilyRepository>(),
            plans, exports, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new RequestHealthReportExportCommand(pet.Id, outsiderId), default);

        result.IsFailure.Should().BeTrue();
        await exports.DidNotReceive().AddAsync(Arg.Any<HealthReportExport>(), Arg.Any<CancellationToken>());
    }
}
