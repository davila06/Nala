using FluentAssertions;
using PawTrack.Application.Auth.Commands.Login;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-48 security regression tests.
///
/// Gap: <c>LoginCommandValidator</c> only checks that <c>Password</c> is not empty:
///
///   <code>
///   RuleFor(x => x.Password)
///       .NotEmpty().WithMessage("Password is required.");
///       // ← MaximumLength missing
///   </code>
///
/// Defence-in-depth risk — bcrypt truncates input at 72 bytes internally, but
/// the full string is still allocated in managed memory before the native bcrypt
/// call.  A crafted request with a multi-megabyte <c>Password</c> field will:
/// <list type="number">
///   <item>Allocate a large string on the managed heap.</item>
///   <item>Pass it to <c>BCrypt.Verify(hugeString, storedHash)</c>.</item>
///   <item>bcrypt processes only the first 72 bytes — the rest is wasted CPU
///   and memory.</item>
/// </list>
///
/// The action-level <c>[RequestSizeLimit(512)]</c> provides the primary defence,
/// but explicit field-level validation is the defence-in-depth layer that
/// remains effective even if the size limit attribute is accidentally removed.
///
/// Fix:
///   Add <c>.MaximumLength(72).WithMessage("Password must not exceed 72 characters.")</c>
///   to the <c>Password</c> rule, matching bcrypt's effective input ceiling.
/// </summary>
public sealed class Round48SecurityRegressionTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Validator_RejectsPassword_LongerThan72Characters()
    {
        // 73 chars — one over bcrypt's effective ceiling
        var password = new string('A', 73);

        var result = _sut.Validate(new LoginCommand("user@example.com", password));

        result.IsValid.Should().BeFalse(
            "a password exceeding 72 characters is beyond bcrypt's effective input length " +
            "and should be rejected at the validation layer as defence-in-depth");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(LoginCommand.Password),
            "the Password field must carry the MaximumLength error");
    }

    [Fact]
    public void Validator_AcceptsPassword_ExactlyAtBcryptLimit()
    {
        // Exactly 72 chars — the bcrypt ceiling itself must be accepted
        var password = new string('A', 72);

        var result = _sut.Validate(new LoginCommand("user@example.com", password));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(LoginCommand.Password),
                "a 72-character password is within bcrypt's effective limit " +
                "and must not trigger MaximumLength validation");
    }

    [Fact]
    public void Validator_RejectsEmptyPassword()
    {
        var result = _sut.Validate(new LoginCommand("user@example.com", string.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginCommand.Password));
    }
}
