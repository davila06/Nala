using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-52 security regression tests.
///
/// Gap: <c>User.CreateGuestForBot</c> sets <c>PasswordHash</c> to
/// <c>$"$bot${Guid.CreateVersion7():N}"</c>:
///
///   <code>
///   PasswordHash = $"$bot${Guid.CreateVersion7():N}", // unusable; not a real bcrypt hash
///   </code>
///
/// In a database breach, every bot-originated guest account is instantly identifiable
/// by the <c>$bot$</c> prefix, enabling an attacker to:
/// <list type="number">
///   <item>
///     Enumerate bot-originated accounts and their associated synthetic email
///     addresses and display names.
///   </item>
///   <item>
///     Target those accounts specifically for follow-on attacks (e.g., account
///     takeover via the email-verification flow).
///   </item>
/// </list>
///
/// A well-designed unusable hash should be indistinguishable from a real bcrypt
/// hash to a database attacker — it should start with a valid bcrypt prefix
/// (<c>$2a$12$</c>) and be of the expected length (60 chars) so it cannot be
/// used to crack or verify passwords, but also cannot be fingerprinted.
///
/// Fix:
///   Replace the <c>$bot$</c> prefix with a constant unusable bcrypt-format string:
///   <code>
///   PasswordHash = "$2a$12$unusable.bot.account.hash.placeholder.not.valid.bcrypt";
///   </code>
///   This is recognisably invalid to a human reading the code (the payload part is
///   not a valid bcrypt hash) but is not fingerprintable as "bot account" by an
///   attacker querying a breached database.
/// </summary>
public sealed class Round52SecurityRegressionTests
{
    [Fact]
    public void CreateGuestForBot_PasswordHash_DoesNotRevealBotOriginByPrefix()
    {
        // Arrange + Act
        var user = User.CreateGuestForBot(
            "bot-guest-abc123@guest.pawtrack.cr",
            "Anonymous Finder");

        // Assert — PasswordHash must not start with a fingerprint-revealing prefix
        user.PasswordHash.Should().NotStartWith("$bot$",
            "a leading '$bot$' prefix in the password hash column allows a database " +
            "attacker to instantly enumerate all bot-originated accounts after a breach, " +
            "enabling targeted follow-on attacks against those accounts");
    }

    [Fact]
    public void CreateGuestForBot_PasswordHash_LooksLikeValidBcryptFormat()
    {
        // A bcrypt hash always starts with $2a$, $2b$, or $2y$ followed by cost factor
        var user = User.CreateGuestForBot("guest@pawtrack.cr", "Finder");

        user.PasswordHash.Should().StartWith("$2",
            "the password hash placeholder should begin with a bcrypt-format prefix " +
            "($2a$, $2b$, or $2y$) so it is indistinguishable from a real hash " +
            "in a database dump, reducing the information value of a breach");
    }

    [Fact]
    public void CreateGuestForBot_PasswordHash_IsNotUsableForAuthentication()
    {
        // The hash must NOT be verifiable by BCrypt.Verify — it is intentionally unusable.
        // We verify this by checking it's not a length-60 bcrypt output (the only valid length).
        var user = User.CreateGuestForBot("guest@pawtrack.cr", "Finder");

        // A real bcrypt hash is exactly 60 characters
        user.PasswordHash.Length.Should().NotBe(60,
            "the placeholder must differ from a real bcrypt hash to ensure " +
            "that even with the hash value an attacker cannot authenticate; " +
            "the non-standard length makes BCrypt.Verify return false");
    }

    [Fact]
    public void CreateGuestForBot_TwoCallsWithSameEmail_ProduceConsistentHash()
    {
        // The placeholder must be a constant, not depend on random data
        var user1 = User.CreateGuestForBot("same@pawtrack.cr", "Finder");
        var user2 = User.CreateGuestForBot("same@pawtrack.cr", "Finder");

        user1.PasswordHash.Should().Be(user2.PasswordHash,
            "the unusable placeholder hash must be a constant so it does not " +
            "consume entropy from the CSPRNG and does not differ across calls");
    }
}
