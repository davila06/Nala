using FluentAssertions;
using PawTrack.Application.Clinics.Commands.RegisterClinic;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-55 security regression tests.
///
/// Gap: <c>RegisterClinicCommandValidator</c> validates <c>ContactEmail</c> with
/// <c>.NotEmpty().EmailAddress()</c> but applies no <c>MaximumLength</c> cap:
///
///   <code>
///   RuleFor(x => x.ContactEmail)
///       .NotEmpty()
///       .EmailAddress()
///       // ← MaximumLength(200) missing
///   </code>
///
/// The EF Core entity configuration constrains the column to
/// <c>HasMaxLength(200)</c>.  A caller can submit a syntactically valid RFC 5321
/// address that is far longer than 200 characters, pass validation, and trigger a
/// DB truncation / <c>DbUpdateException</c> that surfaces as an unhandled HTTP 500.
///
/// Additionally, very long email addresses increase bcrypt hash cost for the
/// user-password binding stored alongside the email, and inflate the JWT
/// <c>email</c> claim, marginally enlarging every authenticated request.
///
/// Fix:
///   Add <c>.MaximumLength(200).WithMessage("Email address must not exceed 200 characters.")</c>
///   before <c>.EmailAddress()</c> in <c>RegisterClinicCommandValidator</c>.
/// </summary>
public sealed class Round55SecurityRegressionTests
{
    private readonly RegisterClinicCommandValidator _sut = new();

    [Fact]
    public void Validator_RejectsContactEmail_ExceedingDbColumnLength()
    {
        // Construct a valid RFC-5321 email whose total length > 200.
        // local-part can be up to 64 chars; domain labels can be long.
        // "a"×64 @ "b"×132 .com → 64 + 1 + 132 + 4 = 201 chars (valid format, too long)
        var email = new string('a', 64) + "@" + new string('b', 132) + ".com"; // 201 chars
        email.Length.Should().BeGreaterThan(200, "pre-condition: email must exceed the 200-char DB cap");

        var result = _sut.Validate(BuildCommand(contactEmail: email));

        result.IsValid.Should().BeFalse(
            "a contact email longer than 200 characters exceeds the DB column " +
            "definition and would cause a DbUpdateException at the INSERT layer");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterClinicCommand.ContactEmail),
            "the ContactEmail field must carry a MaximumLength error");
    }

    [Fact]
    public void Validator_AcceptsContactEmail_AtExactDbColumnLimit()
    {
        // Construct a syntactically valid email of exactly 200 characters.
        // "a"×64 @ "b"×130 .com → 64 + 1 + 130 + 4 = 199 chars — valid and within limit.
        var email = new string('a', 64) + "@" + new string('b', 130) + ".com"; // 199 chars
        email.Length.Should().BeLessThanOrEqualTo(200, "pre-condition: email must be within the cap");

        var result = _sut.Validate(BuildCommand(contactEmail: email));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.ContactEmail)
                          && e.ErrorMessage.Contains("200"),
                "a 199-character email is within the DB column limit");
    }

    [Fact]
    public void Validator_AcceptsTypicalContactEmail()
    {
        var result = _sut.Validate(BuildCommand(contactEmail: "clinic@example.com"));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.ContactEmail));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterClinicCommand BuildCommand(
        string contactEmail = "clinic@example.com",
        string password = "SecurePass1!") =>
        new("Clínica Las Palmas", "SEN-2024-00123", "San José", 9.93m, -84.08m,
            contactEmail, password);
}
