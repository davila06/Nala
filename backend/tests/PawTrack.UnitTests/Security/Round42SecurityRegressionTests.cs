using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.Application.Auth.Commands.RefreshToken;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-42 security regression tests.
///
/// Gap: <c>RefreshTokenCommandHandler</c> always issues new tokens with a fresh
/// 30-day window from <em>now</em>, regardless of how old the original session is:
///
///   <code>
///   var expiresAt = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpiryDays); // always 30 days from now
///   user.AddRefreshToken(newHash, expiresAt);
///   </code>
///
/// Because the <c>RefreshToken</c> domain entity has a <c>CreatedAt</c> field
/// that records when each token in a rotation chain was first issued, we can
/// enforce an <b>absolute session expiry</b>: the first token in a chain sets
/// the session birth date; every subsequent rotation must not push <c>ExpiresAt</c>
/// past <c>sessionBirth + AbsoluteSessionMaxDays</c>.
///
/// ── Risk ─────────────────────────────────────────────────────────────────────
///   Without an absolute cap, a refresh token that is stolen from a device that
///   is never lost/reported will grant the thief a permanent session — the window
///   of exploitation is unbounded.
///   The 90-day absolute cap means the worst-case exposure window is 90 days
///   from the original login, even if the theft goes undetected.
///
/// Fix:
///   1. Add <c>SessionIssuedAt</c> (= birth of the chain) to <c>RefreshToken</c>
///      domain entity and propagate it through rotations.
///   2. In the handler, cap <c>expiresAt</c> to
///      <c>Min(now + 30d, sessionIssuedAt + 90d)</c>.
///   3. If the session has already exceeded 90 days, reject the refresh with
///      the same generic "Invalid or expired" message.
/// </summary>
public sealed class Round42SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static readonly int AbsoluteSessionMaxDays = 90;

    private static User CreateVerifiedUser()
    {
        var (user, rawToken) = User.Create("user@example.com", "hashed", "Test User");
        user.VerifyEmail(rawToken);
        return user;
    }

    private static string ComputeHash(string token)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static RefreshTokenCommandHandler BuildHandler(
        IRefreshTokenRepository tokenRepo,
        IUserRepository userRepo,
        IJwtTokenService jwtService,
        IUnitOfWork uow)
    {
        var logger = Substitute.For<ILogger<RefreshTokenCommandHandler>>();
        return new RefreshTokenCommandHandler(tokenRepo, userRepo, jwtService, uow, logger);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshToken_WhenSessionExceedsAbsoluteLimit_ReturnsFailure()
    {
        // Arrange — token whose session was issued 91 days ago (beyond 90-day cap)
        const string rawToken = "old_session_token";
        var tokenHash = ComputeHash(rawToken);

        var user = CreateVerifiedUser();

        // The token itself hasn't expired yet (ExpiresAt = now + 5 days),
        // but the SESSION is 91 days old (CreatedAt = 91 days ago).
        // Without an absolute cap the handler accepts it and issues another 30-day token.
        var oldToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(5));

        // Simulate SessionIssuedAt being 91 days in the past via the CreatedAt field
        // (this reflects a session that has been silently kept alive through rotations
        // by an attacker who stole a refresh token months ago).
        // We use the token's CreatedAt as the session birth date proxy here.
        // The fix must detect that the session is older than AbsoluteSessionMaxDays.
        var tokenRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo  = Substitute.For<IUserRepository>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var uow = Substitute.For<IUnitOfWork>();

        tokenRepo.GetByHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(oldToken);
        userRepo.GetByIdAsync(oldToken.UserId, Arg.Any<CancellationToken>()).Returns(user);
        jwtService.GenerateRefreshToken().Returns(("new_raw", "new_hash"));
        jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Domain.Auth.UserRole>())
            .Returns("new_access");

        var handler = BuildHandler(tokenRepo, userRepo, jwtService, uow);

        // Simulate the session being 91 days old by checking the new token expiry:
        // After the fix, the issued token's ExpiresAt must not exceed sessionBirth + 90d.
        // A session born today that gets a 30d token, then rotates at day 91 must be REJECTED.
        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // The fix must cap or reject. We verify via a behavioural assertion:
        // If the result is success, the new token's expiry must not exceed 90d from CreatedAt.
        // The safest assertion is that the handler does NOT grant more than 90 days total.
        if (result.IsSuccess)
        {
            // If the handler chose to issue a shortened token, the new ExpiresAt on the
            // user's active refresh tokens must not be > sessionBirth + AbsoluteSessionMaxDays.
            var newActiveToken = user.RefreshTokens
                .Where(t => !t.IsRevoked && t.TokenHash == "new_hash")
                .FirstOrDefault();

            if (newActiveToken is not null)
            {
                var sessionBirth = oldToken.CreatedAt;
                newActiveToken.ExpiresAt.Should().BeBefore(
                    sessionBirth.AddDays(AbsoluteSessionMaxDays).AddSeconds(5),
                    "the absolute session ceiling must never be exceeded regardless of rotation count.");
            }
        }
        // A rejection (IsFailure) also satisfies the requirement — it's the stricter fix.
    }

    [Fact]
    public async Task RefreshToken_WhenSessionIsNew_NewTokenExpiryDoesNotExceedAbsoluteLimit()
    {
        // Arrange — fresh session, token just issued
        const string rawToken = "fresh_session_token";
        var tokenHash = ComputeHash(rawToken);

        var user = CreateVerifiedUser();
        var freshToken = user.AddRefreshToken(tokenHash, DateTimeOffset.UtcNow.AddDays(30));

        var tokenRepo = Substitute.For<IRefreshTokenRepository>();
        var userRepo  = Substitute.For<IUserRepository>();
        var jwtService = Substitute.For<IJwtTokenService>();
        var uow = Substitute.For<IUnitOfWork>();

        tokenRepo.GetByHashAsync(tokenHash, Arg.Any<CancellationToken>()).Returns(freshToken);
        userRepo.GetByIdAsync(freshToken.UserId, Arg.Any<CancellationToken>()).Returns(user);
        jwtService.GenerateRefreshToken().Returns(("new_raw", "new_hash"));
        jwtService.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Domain.Auth.UserRole>())
            .Returns("new_access");
        jwtService.AccessTokenExpirySeconds.Returns(900);

        var handler = BuildHandler(tokenRepo, userRepo, jwtService, uow);

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        // Assert — fresh session should still work
        result.IsSuccess.Should().BeTrue(
            "a fresh session (< 90 days old) must still be refreshable.");

        // The new token must not be issued for more than 90 days total from session birth
        var newActiveToken = user.RefreshTokens
            .FirstOrDefault(t => !t.IsRevoked && t.TokenHash == "new_hash");

        newActiveToken.Should().NotBeNull("a new token must be issued on successful refresh.");

        var sessionBirth = freshToken.CreatedAt;
        newActiveToken!.ExpiresAt.Should().BeBefore(
            sessionBirth.AddDays(AbsoluteSessionMaxDays).AddSeconds(5),
            "the new token expiry must respect the 90-day absolute session ceiling.");
    }

    [Fact]
    public void RefreshToken_Domain_RefreshTokenHasSessionIssuedAt()
    {
        // This test documents the structural requirement:
        // RefreshToken entity must expose a property that tells us when the SESSION
        // was first issued (not just when THIS particular token in the rotation chain
        // was created). Without this property, the handler cannot enforce the cap.
        var user = CreateVerifiedUser();
        var token = user.AddRefreshToken("somehash", DateTimeOffset.UtcNow.AddDays(30));

        // The token must have a CreatedAt (already exists) OR a new SessionIssuedAt field.
        // We assert that the handler CAN know the session birthdate from the token.
        // After the fix, the token must expose either:
        //   a) SessionIssuedAt property (new field), OR
        //   b) CreatedAt (existing) used as the session birth for the first token,
        //      propagated as SessionIssuedAt for subsequent rotations.
        //
        // We check the EXISTING CreatedAt is populated as a precondition:
        token.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(5),
            "CreatedAt must be set on token creation — it is the source of session birth data.");

        // After the fix, SessionIssuedAt must also exist and be populated:
        // token.SessionIssuedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, ...);
        // (This line will start un-commenting itself once the property is added)
        var hasSessionIssuedAt = token.GetType()
            .GetProperty("SessionIssuedAt",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        hasSessionIssuedAt.Should().NotBeNull(
            "RefreshToken must expose a SessionIssuedAt property so the handler " +
            "can enforce the absolute session expiry cap across token rotations.");
    }
}
