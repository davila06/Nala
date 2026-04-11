using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-58 security regression tests.
///
/// Gap: <c>RespondResolveCheckNotificationCommand</c> has no FluentValidation
/// validator registered in the MediatR pipeline.  The command carries two GUID
/// fields (<c>NotificationId</c>, <c>UserId</c>) that must both be non-empty:
///
///   <code>
///   public sealed record RespondResolveCheckNotificationCommand(
///       Guid NotificationId,  // ← no validator guards against Guid.Empty
///       Guid UserId,
///       bool FoundAtHome) : IRequest&lt;Result&lt;bool&gt;&gt;;
///   </code>
///
/// Without a validator, a request carrying <c>Guid.Empty</c> values reaches the
/// handler, which may silently match a notification row with no ID (if one exists
/// due to a bug), update wrong state, or generate a misleading 404 rather than a
/// clear 422 validation error.  A validator is the first line of defence that
/// ensures the handler only ever processes well-formed inputs.
///
/// Fix:
///   Create <c>RespondResolveCheckNotificationCommandValidator.cs</c> in the same
///   folder as the command, with <c>NotEmpty()</c> on both GUID properties.
/// </summary>
public sealed class Round58SecurityRegressionTests
{
    // ── Validator existence (structural) ──────────────────────────────────────

    [Fact]
    public void RespondResolveCheckNotificationCommandValidator_MustExist()
    {
        // The pipeline validator type must be discoverable via reflection.
        // If it doesn't exist, the FluentValidation assembly scan that registers
        // all IValidator<T> implementations will produce no registration for this
        // command, leaving the pipeline unguarded.
        var validatorType = typeof(RespondResolveCheckNotificationCommand)
            .Assembly
            .GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract &&
                t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(RespondResolveCheckNotificationCommand)));

        validatorType.Should().NotBeNull(
            "the MediatR ValidationBehavior pipeline requires an " +
            "IValidator<RespondResolveCheckNotificationCommand> to guard inputs " +
            "before they reach the handler");
    }

    // ── Guid.Empty NotificationId is rejected ─────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyNotificationId()
    {
        var validator = CreateValidator();

        var command = new RespondResolveCheckNotificationCommand(
            Guid.Empty,          // ← invalid
            Guid.NewGuid(),
            true);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse(
            "Guid.Empty is not a valid NotificationId; the handler must not " +
            "attempt a DB lookup with a zero GUID");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RespondResolveCheckNotificationCommand.NotificationId));
    }

    // ── Guid.Empty UserId is rejected ─────────────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyUserId()
    {
        var validator = CreateValidator();

        var command = new RespondResolveCheckNotificationCommand(
            Guid.NewGuid(),
            Guid.Empty,          // ← invalid
            false);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse(
            "Guid.Empty is not a valid UserId; the handler must not attempt an " +
            "ownership check against a zero GUID");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(RespondResolveCheckNotificationCommand.UserId));
    }

    // ── Valid command passes validation ───────────────────────────────────────

    [Fact]
    public void Validator_AcceptsWellFormedCommand()
    {
        var validator = CreateValidator();

        var command = new RespondResolveCheckNotificationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            true);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue(
            "a command with valid GUIDs must pass validation so the handler can run");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IValidator<RespondResolveCheckNotificationCommand> CreateValidator()
    {
        var validatorType = typeof(RespondResolveCheckNotificationCommand)
            .Assembly
            .GetTypes()
            .Single(t =>
                !t.IsAbstract &&
                t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(RespondResolveCheckNotificationCommand)));

        return (IValidator<RespondResolveCheckNotificationCommand>)Activator.CreateInstance(validatorType)!;
    }
}
