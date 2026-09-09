namespace PawTrack.Application.Common.Interfaces;

public interface IWebhookFanout
{
    Task QueueAsync(string eventType, string payload, CancellationToken ct = default);
}