using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class UploadProviderVerificationDocumentCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnerWithPdf_StoresPrivateDocumentReference()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var storage = Substitute.For<IBlobStorageService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming CR", "Cuidado", ServiceProviderCategory.Groomer,
            "Heredia", 10m, -84m, "provider@example.cr");
        providers.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        storage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>())
            .Returns("https://storage.example/provider-verification/document.pdf");
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UploadProviderVerificationDocumentCommandHandler(providers, storage, unitOfWork);
        var result = await handler.Handle(new UploadProviderVerificationDocumentCommand(
            ownerUserId, new byte[] { 0x25, 0x50, 0x44, 0x46 }, "application/pdf"), default);

        result.IsSuccess.Should().BeTrue();
        await storage.Received(1).UploadAsync(
            "provider-verification", Arg.Any<string>(), Arg.Any<Stream>(), "application/pdf", Arg.Any<CancellationToken>());
        await providers.Received(1).AddVerificationAsync(
            Arg.Is<ProviderVerification>(verification => verification.ServiceProviderId == provider.Id && verification.DocumentUrl != null),
            Arg.Any<CancellationToken>());
    }
}