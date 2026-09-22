using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.IssueCertificate;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Certificates;

public sealed class IssueCertificateCommandHandlerTests
{
    [Fact]
    public async Task Handle_CertificateQuotaExhausted_ReturnsFailureBeforeGeneratingArtifact()
    {
        var clinicId = Guid.NewGuid();
        var clinicUserId = Guid.NewGuid();
        var certificates = Substitute.For<ICertificateRepository>();
        var certificateService = Substitute.For<ICertificateService>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var subscription = Subscription.CreateForClinic(clinicId, clinicUserId, SubscriptionTier.ClinicPartner, "PARTNER3", 35_000m);
        subscription.Activate();
        subscriptions.GetActiveForClinicAsync(clinicId, Arg.Any<CancellationToken>()).Returns(subscription);
        certificates.CountForClinicSinceAsync(clinicId, Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>()).Returns(500);

        var handler = new IssueCertificateCommandHandler(certificates, certificateService, subscriptions, unitOfWork);
        var result = await handler.Handle(new IssueCertificateCommand(
            Guid.NewGuid(), clinicId, clinicUserId, CertificateType.HealthClearance, null, null,
            "Max", "Dog", null, "Vet", "SEN", "Dr. Vet"), default);

        result.IsFailure.Should().BeTrue();
        await certificateService.DidNotReceive().GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>());
        await certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }
}
