namespace PawTrack.Application.Common.Interfaces;

public sealed record PushNotificationMetadata(
    string? Url = null,
    string? ResolveCheckNotificationId = null,
    string? Category = null,
    IReadOnlyList<string>? ActionIds = null);

public interface IPushNotificationService
{
    Task SendAsync(
        Guid userId,
        string title,
        string body,
        PushNotificationMetadata? metadata = null,
        CancellationToken cancellationToken = default);
}
