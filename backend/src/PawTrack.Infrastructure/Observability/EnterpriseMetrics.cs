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
}