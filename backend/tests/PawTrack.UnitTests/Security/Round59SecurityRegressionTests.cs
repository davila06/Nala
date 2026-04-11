using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Safety.Commands.GenerateHandoverCode;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-59 security regression tests.
///
/// Gap: <c>GenerateHandoverCodeCommand</c> has no FluentValidation validator.
/// The command carries two GUID fields that must both be non-empty:
///
///   <code>
///   public sealed record GenerateHandoverCodeCommand(
///       Guid LostPetEventId,       // ← no validator guards against Guid.Empty
///       Guid RequestingUserId)
///       : IRequest&lt;Result&lt;string&gt;&gt;;
///   </code>
///
/// Without a validator, a request with <c>Guid.Empty</c> values bypasses the
/// MediatR validation pipeline and enters the handler, where:
///   - A <c>Guid.Empty</c> <c>LostPetEventId</c> triggers a DB query for a
///     non-existent event, wasting a round-trip and surfacing a handler-level
///     error rather than a clean 422.
///   - A <c>Guid.Empty</c> <c>RequestingUserId</c> could (if ownership assertion
///     logic matches a default row) bypass the ownership check.
///
/// Fix:
///   Create <c>GenerateHandoverCodeCommandValidator.cs</c> in the same folder as
///   the command, with <c>NotEmpty()</c> guards on both GUID properties.
/// </summary>
public sealed class Round59SecurityRegressionTests
{
    // ── Validator existence (structural) ──────────────────────────────────────

    [Fact]
    public void GenerateHandoverCodeCommandValidator_MustExist()
    {
        var validatorType = typeof(GenerateHandoverCodeCommand)
            .Assembly
            .GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(GenerateHandoverCodeCommand)));

        validatorType.Should().NotBeNull(
            "the MediatR ValidationBehavior pipeline requires an " +
            "IValidator<GenerateHandoverCodeCommand> to guard inputs " +
            "before they reach the handler");
    }

    // ── Guid.Empty LostPetEventId is rejected ─────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyLostPetEventId()
    {
        var validator = CreateValidator();

        var command = new GenerateHandoverCodeCommand(
            Guid.Empty,          // ← invalid
            Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse(
            "Guid.Empty is not a valid LostPetEventId; the handler must not " +
            "attempt a DB lookup with a zero GUID");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GenerateHandoverCodeCommand.LostPetEventId));
    }

    // ── Guid.Empty RequestingUserId is rejected ───────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyRequestingUserId()
    {
        var validator = CreateValidator();

        var command = new GenerateHandoverCodeCommand(
            Guid.NewGuid(),
            Guid.Empty);         // ← invalid

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse(
            "Guid.Empty is not a valid RequestingUserId; the handler must not " +
            "attempt an ownership check against a zero GUID");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(GenerateHandoverCodeCommand.RequestingUserId));
    }

    // ── Valid command passes validation ───────────────────────────────────────

    [Fact]
    public void Validator_AcceptsWellFormedCommand()
    {
        var validator = CreateValidator();

        var command = new GenerateHandoverCodeCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue(
            "a command with valid GUIDs must pass validation so the handler can run");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IValidator<GenerateHandoverCodeCommand> CreateValidator()
    {
        var validatorType = typeof(GenerateHandoverCodeCommand)
            .Assembly
            .GetTypes()
            .Single(t =>
                !t.IsAbstract &&
                t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(GenerateHandoverCodeCommand)));

        return (IValidator<GenerateHandoverCodeCommand>)Activator.CreateInstance(validatorType)!;
    }
}
