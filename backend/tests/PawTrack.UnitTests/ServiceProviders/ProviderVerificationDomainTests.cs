using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderVerificationDomainTests
{
    [Fact]
    public void Verify_WithPrivateDocumentAndExpiry_ActivatesVerification()
    {
        var verification = ProviderVerification.Submit(Guid.NewGuid(), Guid.NewGuid());
        verification.AttachDocument("https://example.blob.core.windows.net/provider-verification/document.pdf");
        var expiry = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        var result = verification.Verify(Guid.NewGuid(), expiry, "Documentos revisados.");

        result.IsSuccess.Should().BeTrue();
        verification.Status.Should().Be(ProviderVerificationStatus.Verified);
        verification.IsActive.Should().BeTrue();
        verification.ExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void Supersede_VerifiedVerification_IsNoLongerActive()
    {
        var verification = ProviderVerification.Submit(Guid.NewGuid(), Guid.NewGuid());
        verification.AttachDocument("https://example.blob.core.windows.net/provider-verification/document.pdf");
        verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);

        verification.Supersede();

        verification.IsActive.Should().BeFalse();
        verification.SupersededAt.Should().NotBeNull();
    }
}