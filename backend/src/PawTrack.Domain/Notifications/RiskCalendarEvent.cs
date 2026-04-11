namespace PawTrack.Domain.Notifications;

/// <summary>
/// A fixed annual risk event in the Costa Rica calendar that can trigger
/// preventive push/in-app notifications to pet owners.
/// </summary>
public sealed class RiskCalendarEvent
{
    private RiskCalendarEvent() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Display name shown in the notification, e.g. "Tope Nacional".</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Calendar month (1–12).</summary>
    public int Month { get; private set; }

    /// <summary>Day of the month (1–31).</summary>
    public int Day { get; private set; }

    /// <summary>
    /// How many days before the event to send the alert.
    /// 0 = same day, 1 = one day before (most common).
    /// </summary>
    public int DaysBeforeAlert { get; private set; }

    /// <summary>
    /// Notification body template. May reference {EventName}.
    /// Max 300 characters.
    /// </summary>
    public string MessageTemplate { get; private set; } = string.Empty;

    /// <summary>
    /// If set, only users whose canton matches this value receive the alert.
    /// Null = national alert sent to all eligible users.
    /// </summary>
    public string? CantonFilter { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RiskCalendarEvent Create(
        string name,
        int month,
        int day,
        int daysBeforeAlert,
        string messageTemplate,
        string? cantonFilter = null)
    {
        return new RiskCalendarEvent
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            Month = month,
            Day = day,
            DaysBeforeAlert = daysBeforeAlert,
            MessageTemplate = messageTemplate.Trim(),
            CantonFilter = string.IsNullOrWhiteSpace(cantonFilter) ? null : cantonFilter.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Deactivate() => IsActive = false;
    public void Activate()   => IsActive = true;

    /// <summary>
    /// Returns the <see cref="DateOnly"/> on which this event's alert should be dispatched
    /// for the given year.
    /// </summary>
    public DateOnly AlertTriggerDate(int year) =>
        new DateOnly(year, Month, Day).AddDays(-DaysBeforeAlert);
}
