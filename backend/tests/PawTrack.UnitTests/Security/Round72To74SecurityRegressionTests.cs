using FluentAssertions;
using FluentValidation;
using PawTrack.Application.Notifications.Commands.MarkAllNotificationsRead;
using PawTrack.Application.Notifications.Commands.MarkNotificationRead;
using PawTrack.Application.Notifications.Commands.UpdateNotificationPreferences;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-72 through Round-74 security regression tests.
///
/// Gaps: three notification commands lack FluentValidation validators.
///
///   R72 — <c>MarkNotificationReadCommand(Guid NotificationId, Guid RequestingUserId)</c>
///     The handler checks <c>notification.UserId != request.RequestingUserId</c> (ownership).
///     With <c>Guid.Empty</c> for either field the DB lookup or ownership check runs
///     against a zero GUID — wasteful and inconsistent with the project's validator standard.
///
///   R73 — <c>MarkAllNotificationsReadCommand(Guid UserId)</c>
///     <c>Guid.Empty</c> fetches all unread notifications for user 00000000-… (returns
///     empty list), writes nothing, but issues an unnecessary DB query.
///
///   R74 — <c>UpdateNotificationPreferencesCommand(Guid UserId, bool EnablePreventiveAlerts)</c>
///     Same pattern — zero GUID causes a wasted round-trip.
///
/// Fix:
///   Create one <c>*CommandValidator.cs</c> per command with <c>NotEmpty()</c> on all
///   GUID properties.  The <c>bool</c> field in R74 has no invalid state.
/// </summary>
public sealed class Round72To74SecurityRegressionTests
{
    // ═══════════════════════════════════════════════════════════════════════════
    // R72 — MarkNotificationReadCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkNotificationReadCommandValidator_MustExist()
    {
        FindValidator(typeof(MarkNotificationReadCommand)).Should().NotBeNull(
            "MarkNotificationReadCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void MarkNotificationRead_RejectsEmptyNotificationId()
    {
        var result = CreateValidator<MarkNotificationReadCommand>()
            .Validate(new MarkNotificationReadCommand(Guid.Empty, Guid.NewGuid()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MarkNotificationReadCommand.NotificationId));
    }

    [Fact]
    public void MarkNotificationRead_RejectsEmptyRequestingUserId()
    {
        var result = CreateValidator<MarkNotificationReadCommand>()
            .Validate(new MarkNotificationReadCommand(Guid.NewGuid(), Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MarkNotificationReadCommand.RequestingUserId));
    }

    [Fact]
    public void MarkNotificationRead_AcceptsWellFormedCommand()
    {
        var result = CreateValidator<MarkNotificationReadCommand>()
            .Validate(new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R73 — MarkAllNotificationsReadCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MarkAllNotificationsReadCommandValidator_MustExist()
    {
        FindValidator(typeof(MarkAllNotificationsReadCommand)).Should().NotBeNull(
            "MarkAllNotificationsReadCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void MarkAllNotificationsRead_RejectsEmptyUserId()
    {
        var result = CreateValidator<MarkAllNotificationsReadCommand>()
            .Validate(new MarkAllNotificationsReadCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(MarkAllNotificationsReadCommand.UserId));
    }

    [Fact]
    public void MarkAllNotificationsRead_AcceptsValidUserId()
    {
        var result = CreateValidator<MarkAllNotificationsReadCommand>()
            .Validate(new MarkAllNotificationsReadCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // R74 — UpdateNotificationPreferencesCommand
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void UpdateNotificationPreferencesCommandValidator_MustExist()
    {
        FindValidator(typeof(UpdateNotificationPreferencesCommand)).Should().NotBeNull(
            "UpdateNotificationPreferencesCommand must have an IValidator<T> registration");
    }

    [Fact]
    public void UpdateNotificationPreferences_RejectsEmptyUserId()
    {
        var result = CreateValidator<UpdateNotificationPreferencesCommand>()
            .Validate(new UpdateNotificationPreferencesCommand(Guid.Empty, true));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(UpdateNotificationPreferencesCommand.UserId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateNotificationPreferences_AcceptsAnyBoolValue(bool enableAlerts)
    {
        var result = CreateValidator<UpdateNotificationPreferencesCommand>()
            .Validate(new UpdateNotificationPreferencesCommand(Guid.NewGuid(), enableAlerts));
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
