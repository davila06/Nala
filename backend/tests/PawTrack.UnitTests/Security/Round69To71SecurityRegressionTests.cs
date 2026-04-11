using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Allies.Commands.ReviewAllyApplication;
using PawTrack.Application.Chat.Commands.OpenChatThread;
using PawTrack.Application.Clinics.Commands.ReviewClinic;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-69 through Round-71 security regression tests.
///
/// Gaps: three commands have no FluentValidation validators:
///
///   R69 — <c>OpenChatThreadCommand(Guid LostPetEventId, Guid InitiatorUserId)</c>
///     Without validation, a <c>Guid.Empty</c> LostPetEventId queries the repository
///     for event 00000000-… and the handler returns a graceful failure — but bypasses
///     the structured validation response and wastes a DB round-trip.
///
///   R70 — <c>ReviewAllyApplicationCommand(Guid UserId, bool Approve)</c>
///     Admin-only action.  <c>Guid.Empty</c> UserId reaches the repository lookup;
///     no user is found and a graceful 422 is returned, but the validation pipeline
///     is a first line of defence that should be consistent across all commands.
///
///   R71 — <c>ReviewClinicCommand(Guid ClinicId, bool Approve)</c>
///     Same pattern — admin operation, GUID should never be empty.
///
/// Fix:
///   Create one validator per command with <c>NotEmpty()</c> on all GUID properties.
///   Boolean fields (<c>Approve</c>) have no meaningful invalid state and must NOT
///   be validated.
/// </summary>
public sealed class Round69To71SecurityRegressionTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // R69 — OpenChatThreadCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OpenChatThreadCommandValidator_MustExist()
    {
        FindValidator(typeof(OpenChatThreadCommand)).Should().NotBeNull(
            "OpenChatThreadCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void OpenChatThread_RejectsEmptyLostPetEventId()
    {
        var result = CreateValidator<OpenChatThreadCommand>()
            .Validate(new OpenChatThreadCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(OpenChatThreadCommand.LostPetEventId));
    }

    [Fact]
    public void OpenChatThread_RejectsEmptyInitiatorUserId()
    {
        var result = CreateValidator<OpenChatThreadCommand>()
            .Validate(new OpenChatThreadCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(OpenChatThreadCommand.InitiatorUserId));
    }

    [Fact]
    public void OpenChatThread_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<OpenChatThreadCommand>()
            .Validate(new OpenChatThreadCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R70 — ReviewAllyApplicationCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ReviewAllyApplicationCommandValidator_MustExist()
    {
        FindValidator(typeof(ReviewAllyApplicationCommand)).Should().NotBeNull(
            "ReviewAllyApplicationCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ReviewAllyApplication_RejectsEmptyUserId()
    {
        var result = CreateValidator<ReviewAllyApplicationCommand>()
            .Validate(new ReviewAllyApplicationCommand(Guid.Empty, true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReviewAllyApplicationCommand.UserId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReviewAllyApplication_AcceptsAnyApproveValue(bool approve)
    {
        // bool has no invalid state; both true and false must pass
        var result = CreateValidator<ReviewAllyApplicationCommand>()
            .Validate(new ReviewAllyApplicationCommand(Guid.NewGuid(), approve));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R71 — ReviewClinicCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ReviewClinicCommandValidator_MustExist()
    {
        FindValidator(typeof(ReviewClinicCommand)).Should().NotBeNull(
            "ReviewClinicCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ReviewClinic_RejectsEmptyClinicId()
    {
        var result = CreateValidator<ReviewClinicCommand>()
            .Validate(new ReviewClinicCommand(Guid.Empty, true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReviewClinicCommand.ClinicId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReviewClinic_AcceptsAnyApproveValue(bool approve)
    {
        var result = CreateValidator<ReviewClinicCommand>()
            .Validate(new ReviewClinicCommand(Guid.NewGuid(), approve));
        result.IsValid.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Type? FindValidator(Type commandType) =>
        commandType.Assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract && t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == commandType));

    private static IValidator<T> CreateValidator<T>()
    {
        var type = FindValidator(typeof(T))
            ?? throw new InvalidOperationException($"No IValidator<{typeof(T).Name}> found.");
        return (IValidator<T>)Activator.CreateInstance(type)!;
    }
}
