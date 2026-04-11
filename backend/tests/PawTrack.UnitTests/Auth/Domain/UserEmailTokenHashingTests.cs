using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Domain;

/// <summary>
/// Round-6 security: email verification tokens must be stored as SHA-256 hashes,
/// not as plaintext, so a DB breach cannot be leveraged to verify arbitrary accounts
/// within the 24-hour token window.
/// </summary>
public sealed class UserEmailTokenHashingTests
{
    // ── User.Create ───────────────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsRawToken_AndStoresHash()
    {
        var (user, rawToken) = User.Create("test@example.com", "pw", "Alice");

        rawToken.Should().NotBeNullOrWhiteSpace("a CSPRNG raw token must be returned");

        // The stored value must differ from the raw token (it is a hex hash)
        user.EmailVerificationToken.Should().NotBeNull();
        user.EmailVerificationToken.Should().NotBe(rawToken,
            "the DB column must store the hash, not the raw token");
    }

    [Fact]
    public void Create_StoredValue_EqualsExpectedHash()
    {
        var (user, rawToken) = User.Create("test@example.com", "pw", "Alice");

        var expectedHash = User.ToHexHash(rawToken);
        user.EmailVerificationToken.Should().Be(expectedHash,
            "the stored value must be the SHA-256 hex hash of the raw token");
    }

    [Fact]
    public void Create_RawToken_HasHighEntropy()
    {
        var (_, rawToken) = User.Create("test@example.com", "pw", "Alice");

        // Base64url of 32 random bytes → 43 characters, all URL-safe
        rawToken.Length.Should().BeGreaterThanOrEqualTo(40,
            "32-byte CSPRNG base64url should be at least 40 characters");
    }

    [Fact]
    public void Create_TwoInvocations_ProduceDifferentTokens()
    {
        var (_, rawToken1) = User.Create("a@example.com", "pw", "Alice");
        var (_, rawToken2) = User.Create("b@example.com", "pw", "Bob");

        rawToken1.Should().NotBe(rawToken2, "each token must be cryptographically unique");
    }

    // ── User.VerifyEmail ──────────────────────────────────────────────────────

    [Fact]
    public void VerifyEmail_WithCorrectRawToken_Succeeds()
    {
        var (user, rawToken) = User.Create("test@example.com", "pw", "Alice");

        var result = user.VerifyEmail(rawToken);

        result.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmail_WithStoredHashDirectly_Fails()
    {
        // Prove that passing the hash (as an attacker with DB access would) does NOT succeed.
        var (user, _) = User.Create("test@example.com", "pw", "Alice");
        var storedHash = user.EmailVerificationToken!;

        var result = user.VerifyEmail(storedHash);

        result.Should().BeFalse("the stored hash is not the correct raw token");
    }

    [Fact]
    public void VerifyEmail_WithWrongToken_Fails()
    {
        var (user, _) = User.Create("test@example.com", "pw", "Alice");

        var result = user.VerifyEmail("this-is-not-the-right-token");

        result.Should().BeFalse();
        user.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public void VerifyEmail_WhenAlreadyVerified_ReturnsFalse()
    {
        var (user, rawToken) = User.Create("test@example.com", "pw", "Alice");
        user.VerifyEmail(rawToken);

        // Second attempt with the same (correct) token must fail — token is consumed
        var result = user.VerifyEmail(rawToken);

        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyEmail_ClearsTokenAfterSuccess()
    {
        var (user, rawToken) = User.Create("test@example.com", "pw", "Alice");
        user.VerifyEmail(rawToken);

        user.EmailVerificationToken.Should().BeNull("token must be cleared after successful verification");
        user.EmailVerificationTokenExpiry.Should().BeNull();
    }

    // ── Token hash helper ─────────────────────────────────────────────────────

    [Fact]
    public void ToHexHash_SameInput_SameOutput()
    {
        var h1 = User.ToHexHash("hello");
        var h2 = User.ToHexHash("hello");
        h1.Should().Be(h2, "SHA-256 is deterministic");
    }

    [Fact]
    public void ToHexHash_DifferentInputs_DifferentOutputs()
    {
        var h1 = User.ToHexHash("token-a");
        var h2 = User.ToHexHash("token-b");
        h1.Should().NotBe(h2);
    }
}
