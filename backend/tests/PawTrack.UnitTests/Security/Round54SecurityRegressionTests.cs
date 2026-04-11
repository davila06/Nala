using FluentAssertions;
using PawTrack.Application.Clinics.Commands.RegisterClinic;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-54 security regression tests.
///
/// Gap: <c>RegisterClinicCommandValidator</c> permits <c>LicenseNumber</c> up to
/// 100 characters, but the EF Core entity configuration constrains the column to
/// <c>HasMaxLength(50)</c>:
///
///   <code>
///   // Validator (ValidatorFile line ≈ 22)
///   RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(100)...
///
///   // ClinicEntityConfiguration (line ≈ 29)
///   builder.Property(c => c.LicenseNumber).HasMaxLength(50).IsRequired();
///   </code>
///
/// A caller who registers with a LicenseNumber between 51 and 100 characters long
/// passes validation and reaches the <c>INSERT</c> statement, which then throws a
/// <c>DbUpdateException</c> (SQL string truncation) that is unhandled at the
/// business layer and surfaces as HTTP 500.
///
/// CVSS-equivalent: Medium — denial-of-service path (unhandled exception) + data
/// integrity gap if the DB-level constraint is relaxed in future.
///
/// Fix:
///   Change <c>.MaximumLength(100)</c> to <c>.MaximumLength(50)</c> in
///   <c>RegisterClinicCommandValidator</c> to match the DB column definition.
/// </summary>
public sealed class Round54SecurityRegressionTests
{
    private readonly RegisterClinicCommandValidator _sut = new();

    [Fact]
    public void Validator_RejectsLicenseNumber_Exceeding50Characters()
    {
        // 51 chars — one character over the DB column cap
        var licenseNumber = new string('A', 51);

        var result = _sut.Validate(BuildCommand(licenseNumber: licenseNumber));

        result.IsValid.Should().BeFalse(
            "a SENASA license number longer than 50 characters exceeds the DB column " +
            "definition and would cause a DbUpdateException at the INSERT layer");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RegisterClinicCommand.LicenseNumber),
            "the LicenseNumber field must carry the MaximumLength(50) error");
    }

    [Fact]
    public void Validator_AcceptsLicenseNumber_AtExactDbColumnLimit()
    {
        // Exactly 50 chars — must be within the validator boundary
        var licenseNumber = new string('A', 50);

        var result = _sut.Validate(BuildCommand(licenseNumber: licenseNumber));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.LicenseNumber)
                          && e.ErrorMessage.Contains("50"),
                "a 50-character license number is within the DB column limit");
    }

    [Fact]
    public void Validator_AcceptsTypicalLicenseNumber()
    {
        // Realistic SENASA format
        var result = _sut.Validate(BuildCommand(licenseNumber: "SEN-2024-00123"));

        result.Errors.Should()
            .NotContain(e => e.PropertyName == nameof(RegisterClinicCommand.LicenseNumber));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterClinicCommand BuildCommand(
        string licenseNumber = "SEN-2024-00123",
        string password = "SecurePass1!") =>
        new("Clínica Las Palmas", licenseNumber, "San José", 9.93m, -84.08m,
            "clinic@example.com", password);
}
