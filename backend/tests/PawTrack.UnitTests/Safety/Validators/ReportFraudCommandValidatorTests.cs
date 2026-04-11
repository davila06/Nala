using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Safety.Commands.ReportFraud;
using PawTrack.Domain.Safety;

namespace PawTrack.UnitTests.Safety.Validators;

/// <summary>
/// Round-7 security: ReportFraudCommand must cap the Description field to prevent
/// megabyte payloads from reaching the DB or consuming excessive memory.
/// </summary>
public sealed class ReportFraudCommandValidatorTests
{
    private readonly ReportFraudCommandValidator _sut = new();

    private static ReportFraudCommand Valid(string? description = null) =>
        new(null, "127.0.0.1", FraudContext.PublicProfile, Guid.NewGuid(), null, description);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _sut.TestValidate(Valid("Suspicious behavior reported."));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullDescription_PassesValidation()
    {
        var result = _sut.TestValidate(Valid(null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DescriptionAtExactMaxLength_Passes()
    {
        var desc = new string('x', 1000);
        var result = _sut.TestValidate(Valid(desc));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DescriptionExceedsMaxLength_FailsValidation()
    {
        var desc = new string('x', 1001);
        var result = _sut.TestValidate(Valid(desc));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NoTargetNoRelatedEntity_FailsValidation()
    {
        // Without a target or entity, the report cannot be acted upon.
        var cmd = new ReportFraudCommand(null, "127.0.0.1", FraudContext.PublicProfile, null, null, "Some text");
        var result = _sut.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor("ReportTarget");
    }

    [Fact]
    public void Validate_OnlyTargetUserId_Passes()
    {
        var cmd = new ReportFraudCommand(null, "127.0.0.1", FraudContext.PublicProfile, null, Guid.NewGuid(), null);
        var result = _sut.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
