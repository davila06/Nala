using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Pets.Commands.DeletePet;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-64 security regression tests.
///
/// Gap: <c>DeletePetCommand</c> has no FluentValidation validator.
/// The command carries two GUIDs that must both be non-empty:
///
///   <code>
///   public sealed record DeletePetCommand(Guid PetId, Guid RequestingUserId)
///       : IRequest&lt;Result&lt;bool&gt;&gt;;
///   </code>
///
/// Without a validator, a request with <c>Guid.Empty</c> values reaches the
/// handler.  The handler queries the DB <c>GetByIdAsync(Guid.Empty)</c> which may:
///   - Return <c>null</c> → graceful "not found" failure.
///   - Match a corrupted/seed row with a zero primary key in a non-production DB
///     → unintended deletion of that row.
///
/// The ownership check <c>pet.OwnerId != request.RequestingUserId</c> also runs
/// against <c>Guid.Empty</c>, which should never represent a real user.
///
/// Fix:
///   Create <c>DeletePetCommandValidator.cs</c> with <c>NotEmpty()</c> on both
///   <c>PetId</c> and <c>RequestingUserId</c>.
/// </summary>
public sealed class Round64SecurityRegressionTests
{
    // ── Validator existence ───────────────────────────────────────────────────

    [Fact]
    public void DeletePetCommandValidator_MustExist()
    {
        FindValidatorType().Should().NotBeNull(
            "the MediatR ValidationBehavior pipeline requires an " +
            "IValidator<DeletePetCommand> to guard PetId and RequestingUserId");
    }

    // ── Guid.Empty PetId rejected ─────────────────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyPetId()
    {
        var result = CreateValidator().Validate(
            new DeletePetCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse(
            "Guid.Empty PetId must be rejected before a DB delete is attempted");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DeletePetCommand.PetId));
    }

    // ── Guid.Empty RequestingUserId rejected ──────────────────────────────────

    [Fact]
    public void Validator_RejectsEmptyRequestingUserId()
    {
        var result = CreateValidator().Validate(
            new DeletePetCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse(
            "Guid.Empty RequestingUserId must be rejected — the ownership check " +
            "must never run against a zero GUID");

        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(DeletePetCommand.RequestingUserId));
    }

    // ── Valid command passes ──────────────────────────────────────────────────

    [Fact]
    public void Validator_AcceptsWellFormedCommand()
    {
        var result = CreateValidator().Validate(
            new DeletePetCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsValid.Should().BeTrue(
            "a command with valid GUIDs must pass validation");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Type? FindValidatorType() =>
        typeof(DeletePetCommand).Assembly.GetTypes()
            .FirstOrDefault(t =>
                !t.IsAbstract && t.IsClass &&
                t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>) &&
                    i.GetGenericArguments()[0] == typeof(DeletePetCommand)));

    private static IValidator<DeletePetCommand> CreateValidator()
    {
        var type = FindValidatorType()
            ?? throw new InvalidOperationException(
                "No IValidator<DeletePetCommand> found.");
        return (IValidator<DeletePetCommand>)Activator.CreateInstance(type)!;
    }
}
