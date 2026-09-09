using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Domain;

public sealed class WebAuthnCredentialTests
{
    [Fact]
    public void Create_StoresCredentialMaterialAndCounter()
    {
        var userId = Guid.NewGuid();
        var credentialId = new byte[] { 1, 2, 3 };
        var publicKey = new byte[] { 4, 5, 6 };

        var credential = WebAuthnCredential.Create(userId, credentialId, publicKey, 7, "Laptop");

        credential.UserId.Should().Be(userId);
        credential.CredentialId.Should().BeEquivalentTo(credentialId);
        credential.PublicKey.Should().BeEquivalentTo(publicKey);
        credential.SignatureCounter.Should().Be(7);
        credential.DeviceName.Should().Be("Laptop");
        credential.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateCounter_RejectsRollbackAndRevokeDisablesCredential()
    {
        var credential = WebAuthnCredential.Create(Guid.NewGuid(), new byte[] { 1 }, new byte[] { 2 }, 10);

        credential.UpdateCounter(9).Should().BeFalse();
        credential.UpdateCounter(11).Should().BeTrue();
        credential.Revoke();

        credential.IsActive.Should().BeFalse();
        credential.UpdateCounter(12).Should().BeFalse();
    }
}