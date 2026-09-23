using System.Diagnostics.Metrics;

namespace PawTrack.Infrastructure.Observability;

public static class EnterpriseMetrics
{
    public static readonly Meter Meter = new("PawTrack.Enterprise", "1.0.0");
    public static readonly Counter<long> WebhookQueued = Meter.CreateCounter<long>("pawtrack.webhook.queued");
    public static readonly Counter<long> WebhookDelivered = Meter.CreateCounter<long>("pawtrack.webhook.delivered");
    public static readonly Counter<long> WebhookFailed = Meter.CreateCounter<long>("pawtrack.webhook.failed");
    public static readonly Counter<long> MedicalExports = Meter.CreateCounter<long>("pawtrack.medical.exported");
    public static readonly Histogram<double> WebhookDeliveryDurationMs = Meter.CreateHistogram<double>("pawtrack.webhook.delivery.duration_ms");
    public static readonly Counter<long> ApiRequests = Meter.CreateCounter<long>("pawtrack.api.requests");
    public static readonly Counter<long> ApiErrors = Meter.CreateCounter<long>("pawtrack.api.errors");
    public static readonly Histogram<double> ApiRequestDurationMs = Meter.CreateHistogram<double>("pawtrack.api.request.duration_ms");
    public static readonly Counter<long> ProductEventsIngested = Meter.CreateCounter<long>("pawtrack.product.events.ingested");
    public static readonly Counter<long> ProductFunnelQueries = Meter.CreateCounter<long>("pawtrack.product.funnel.queries");
    public static readonly Histogram<long> NorthStarActiveProtectedPets =
        Meter.CreateHistogram<long>("pawtrack.north_star.active_protected_pets");
    public static readonly Counter<long> ExternalProviderRequests =
        Meter.CreateCounter<long>("pawtrack.provider.requests");
    public static readonly Counter<long> ExternalProviderFailures =
        Meter.CreateCounter<long>("pawtrack.provider.failures");
    public static readonly Histogram<double> ExternalProviderDurationMs =
        Meter.CreateHistogram<double>("pawtrack.provider.duration_ms");
    public static readonly Counter<long> JobRuns = Meter.CreateCounter<long>("pawtrack.job.runs");
    public static readonly Counter<long> JobFailures = Meter.CreateCounter<long>("pawtrack.job.failures");
    public static readonly Histogram<double> JobDurationMs = Meter.CreateHistogram<double>("pawtrack.job.duration_ms");
    public static readonly Counter<long> OutboxPending = Meter.CreateCounter<long>("pawtrack.outbox.pending");
    public static readonly Counter<long> OutboxProcessed = Meter.CreateCounter<long>("pawtrack.outbox.processed");
    public static readonly Counter<long> OutboxFailed = Meter.CreateCounter<long>("pawtrack.outbox.failed");
    public static readonly Counter<long> SignalRJoins = Meter.CreateCounter<long>("pawtrack.signalr.joins");
    public static readonly Counter<long> SignalRRejected = Meter.CreateCounter<long>("pawtrack.signalr.rejected");
    public static readonly Counter<long> SignalRBroadcasts = Meter.CreateCounter<long>("pawtrack.signalr.broadcasts");
}
