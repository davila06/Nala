namespace PawTrack.Domain.Notifications;

/// <summary>
/// Stores a user's opt-in/opt-out preferences for each notification category.
/// Created on-demand (first GET or first update). Default: all alerts enabled.
/// </summary>
public sealed class UserNotificationPreferences
{
    private UserNotificationPreferences() { } // EF Core

    /// <summary>Same as <see cref="PawTrack.Domain.Auth.User.Id"/> — this is a 1-to-1 mapping.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Preventive risk-calendar alerts (Tope, Año Nuevo, etc.).</summary>
    public bool EnablePreventiveAlerts { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserNotificationPreferences CreateDefault(Guid userId) =>
        new()
        {
            UserId = userId,
            EnablePreventiveAlerts = true,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    public void UpdatePreventiveAlerts(bool enabled)
    {
        EnablePreventiveAlerts = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
