using FluentAssertions;
using NSubstitute;
using PawTrack.Application.AnimalWelfare.Interfaces;
using PawTrack.Application.AnimalWelfare.Queries;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.UnitTests.AnimalWelfare;

public sealed class DownloadWelfareEvidenceQueryTests
{
    [Fact]
    public async Task Handle_RejectsActorAssignedToAnotherWelfareCase()
    {
        var evidenceRepository = Substitute.For<IAnimalWelfareEvidenceRepository>();
        var caseRepository = Substitute.For<IAnimalWelfareCaseRepository>();
        var auditRepository = Substitute.For<IAnimalWelfareAuditRepository>();
        var blobStorage = Substitute.For<IBlobStorageService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var actorId = Guid.NewGuid();
        var assignedId = Guid.NewGuid();
        var welfareCase = AnimalWelfareCase.Create(
            WelfareCaseType.Neglect, WelfareSeverity.High, "San Jose", "Caso sanitizado",
            null, true, null, null);
        welfareCase.AssignTo(assignedId, "Municipality", assignedId);
        var evidence = AnimalWelfareEvidence.Create(
            welfareCase.Id, "private/evidence.jpg", "image/jpeg", 12,
            WelfareEvidenceKind.Photo, assignedId, true, null);
        evidenceRepository.GetByIdAsync(evidence.Id, Arg.Any<CancellationToken>()).Returns(evidence);
        caseRepository.GetByIdAsync(welfareCase.Id, Arg.Any<CancellationToken>()).Returns(welfareCase);
        var handler = new DownloadWelfareEvidenceQueryHandler(
            evidenceRepository, caseRepository, auditRepository, blobStorage, unitOfWork);

        var result = await handler.Handle(new DownloadWelfareEvidenceQuery(evidence.Id, actorId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await blobStorage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await auditRepository.DidNotReceive().AddAsync(Arg.Any<AnimalWelfareCaseAuditLog>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllowsAssignedActorToDownloadEvidence()
    {
        var evidenceRepository = Substitute.For<IAnimalWelfareEvidenceRepository>();
        var caseRepository = Substitute.For<IAnimalWelfareCaseRepository>();
        var auditRepository = Substitute.For<IAnimalWelfareAuditRepository>();
        var blobStorage = Substitute.For<IBlobStorageService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var actorId = Guid.NewGuid();
        var welfareCase = AnimalWelfareCase.Create(
            WelfareCaseType.Neglect, WelfareSeverity.High, "San Jose", "Caso sanitizado",
            null, true, null, null);
        welfareCase.AssignTo(actorId, "Municipality", actorId);
        var evidence = AnimalWelfareEvidence.Create(
            welfareCase.Id, "private/evidence.jpg", "image/jpeg", 12,
            WelfareEvidenceKind.Photo, actorId, true, null);
        evidenceRepository.GetByIdAsync(evidence.Id, Arg.Any<CancellationToken>()).Returns(evidence);
        caseRepository.GetByIdAsync(welfareCase.Id, Arg.Any<CancellationToken>()).Returns(welfareCase);
        blobStorage.DownloadAsync(evidence.BlobUrl, Arg.Any<CancellationToken>()).Returns([1, 2, 3]);
        var handler = new DownloadWelfareEvidenceQueryHandler(
            evidenceRepository, caseRepository, auditRepository, blobStorage, unitOfWork);

        var result = await handler.Handle(new DownloadWelfareEvidenceQuery(evidence.Id, actorId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bytes.Should().Equal(1, 2, 3);
        await auditRepository.Received(1).AddAsync(Arg.Any<AnimalWelfareCaseAuditLog>(), Arg.Any<CancellationToken>());
    }
}
