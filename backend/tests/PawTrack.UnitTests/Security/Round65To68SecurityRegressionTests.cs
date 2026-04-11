using FluentAssertions;
using FluentValidation;
using PawTrack.Application.LostPets.Commands.ActivateSearchCoordination;
using PawTrack.Application.LostPets.Commands.ClaimZone;
using PawTrack.Application.LostPets.Commands.ClearZone;
using PawTrack.Application.LostPets.Commands.ReleaseZone;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-65 through Round-68 security regression tests.
///
/// Gap: four zone-management commands lack FluentValidation validators:
///   - <c>ActivateSearchCoordinationCommand(Guid LostPetEventId, Guid RequestingUserId)</c>
///   - <c>ClaimZoneCommand(Guid ZoneId, Guid UserId)</c>
///   - <c>ClearZoneCommand(Guid ZoneId, Guid UserId)</c>
///   - <c>ReleaseZoneCommand(Guid ZoneId, Guid UserId)</c>
///
/// These commands are invoked from SignalR hub methods that already apply a participant
/// gate (<c>IsSearchParticipantQuery</c>).  However, the hub gate runs AFTER command
/// construction — a <c>Guid.Empty</c> ZoneId/LostPetEventId reaches the handler and
/// issues a DB lookup for a non-existent row.  Defence-in-depth requires that the
/// validator rejects zero GUIDs before the handler is ever entered.
///
/// Fix:
///   Create one <c>*CommandValidator.cs</c> per command with <c>NotEmpty()</c> on all
///   GUID properties.
/// </summary>
public sealed class Round65To68SecurityRegressionTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // R65 — ActivateSearchCoordinationCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ActivateSearchCoordinationCommandValidator_MustExist()
    {
        FindValidator(typeof(ActivateSearchCoordinationCommand)).Should().NotBeNull(
            "ActivateSearchCoordinationCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ActivateSearchCoordination_RejectsEmptyLostPetEventId()
    {
        var result = CreateValidator<ActivateSearchCoordinationCommand>()
            .Validate(new ActivateSearchCoordinationCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ActivateSearchCoordinationCommand.LostPetEventId));
    }

    [Fact]
    public void ActivateSearchCoordination_RejectsEmptyRequestingUserId()
    {
        var result = CreateValidator<ActivateSearchCoordinationCommand>()
            .Validate(new ActivateSearchCoordinationCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ActivateSearchCoordinationCommand.RequestingUserId));
    }

    [Fact]
    public void ActivateSearchCoordination_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<ActivateSearchCoordinationCommand>()
            .Validate(new ActivateSearchCoordinationCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R66 — ClaimZoneCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ClaimZoneCommandValidator_MustExist()
    {
        FindValidator(typeof(ClaimZoneCommand)).Should().NotBeNull(
            "ClaimZoneCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ClaimZone_RejectsEmptyZoneId()
    {
        var result = CreateValidator<ClaimZoneCommand>()
            .Validate(new ClaimZoneCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ClaimZoneCommand.ZoneId));
    }

    [Fact]
    public void ClaimZone_RejectsEmptyUserId()
    {
        var result = CreateValidator<ClaimZoneCommand>()
            .Validate(new ClaimZoneCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ClaimZoneCommand.UserId));
    }

    [Fact]
    public void ClaimZone_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<ClaimZoneCommand>()
            .Validate(new ClaimZoneCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R67 — ClearZoneCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ClearZoneCommandValidator_MustExist()
    {
        FindValidator(typeof(ClearZoneCommand)).Should().NotBeNull(
            "ClearZoneCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ClearZone_RejectsEmptyZoneId()
    {
        var result = CreateValidator<ClearZoneCommand>()
            .Validate(new ClearZoneCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ClearZoneCommand.ZoneId));
    }

    [Fact]
    public void ClearZone_RejectsEmptyUserId()
    {
        var result = CreateValidator<ClearZoneCommand>()
            .Validate(new ClearZoneCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ClearZoneCommand.UserId));
    }

    [Fact]
    public void ClearZone_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<ClearZoneCommand>()
            .Validate(new ClearZoneCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R68 — ReleaseZoneCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ReleaseZoneCommandValidator_MustExist()
    {
        FindValidator(typeof(ReleaseZoneCommand)).Should().NotBeNull(
            "ReleaseZoneCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void ReleaseZone_RejectsEmptyZoneId()
    {
        var result = CreateValidator<ReleaseZoneCommand>()
            .Validate(new ReleaseZoneCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReleaseZoneCommand.ZoneId));
    }

    [Fact]
    public void ReleaseZone_RejectsEmptyUserId()
    {
        var result = CreateValidator<ReleaseZoneCommand>()
            .Validate(new ReleaseZoneCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ReleaseZoneCommand.UserId));
    }

    [Fact]
    public void ReleaseZone_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<ReleaseZoneCommand>()
            .Validate(new ReleaseZoneCommand(Guid.NewGuid(), Guid.NewGuid()));
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
