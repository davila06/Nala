using FluentAssertions;
using PawTrack.Application.Clinics.Commands.RegisterClinic;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-53 security regression tests.
///
/// Gap: <c>RegisterClinicCommandValidator</c> enforces a rich password policy
/// (min 8, upper, lower, digit, special) but has no upper bound:
///
///   <code>
///   RuleFor(x => x.Password)
///       .NotEmpty()
///       .MinimumLength(8)
///       .Matches(@"[A-Z]")...
///       // ← MaximumLength(72) missing
///   </code>
///
/// bcrypt silently truncates input at 72 bytes.  A clinic owner who registers with
/// a 200-char password produces the same bcrypt hash as someone who knows only the
/// first 72 chars.  Any attacker who guesses those 72 chars authenticates
/// successfully — the remaining chars are cryptographically invisible.
///
/// R48 fixed the same gap in <c>LoginCommandValidator</c>.  The clinic registration
/// path was missed.
///
/// Fix:
///   Add <c>.MaximumLength(72).WithMessage("Password must not exceed 72 characters.")</c>
///   immediately after <c>.MinimumLength(8)</c>.
/// </summary>
public sealed class Round53SecurityRegressionTests
{
    private readonly RegisterClinicCommandValidator _sut = new();

    [Fact]
    public void Validator_RejectsPassword_LongerThan72Characters()
    {
        // 73 chars — one byte over bcrypt's effective ceiling
        var password = "Aa1!" + new string('x', 69); // 73 chars, satisfies other rules

        var result = _sut.Validate(BuildCommand(password: password));

        result.IsValid.Should().BeFalse(
            "a clinic password exceeding 72 characters is beyond bcrypt's effective input length " +
            "and should be rejected at the validation layer to prevent silent truncation attacks");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterClinicCommand.Password),
            "the Password field must carry the MaximumLength error");
    }

    [Fact]
    public void Validator_AcceptsPassword_ExactlyAtBcryptLimit()
    {
        // Exactly 72 chars — the bcrypt ceiling itself must be accepted
        var password = "Aa1!" + new string('x', 68); // 72 chars

        var result = _sut.Validate(BuildCommand(password: password));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.Password)
                          && e.ErrorMessage.Contains("72"),
                "a 72-character password is within bcrypt's effective limit");
    }

    [Fact]
    public void Validator_AcceptsStrongPasswordUnder72Characters()
    {
        var result = _sut.Validate(BuildCommand(password: "SecurePass1!"));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.Password));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterClinicCommand BuildCommand(string password = "SecurePass1!") =>
        new("Clínica Las Palmas", "SEN-00001", "San José", 9.93m, -84.08m,
            "clinic@example.com", password);
}
