namespace PawTrack.Domain.Audit;

public enum AuditAction
{
    // Allies
    AllyApproved,
    AllyRejected,

    // Clinics
    ClinicApproved,
    ClinicRejected,

    // Subscriptions
    SubscriptionActivated,
    SubscriptionCancelled,

    // Adoptions
    AnimalRemoved,
    AnimalPaused,
    AnimalRestored,

    // Stores
    StoreApproved,
    StoreRejected,
    StoreSuspended,

    // Service providers
    ServiceProviderApproved,
    ServiceProviderRejected,
    ServiceProviderSuspended,
    ProviderBookingConfirmed,
    ProviderBookingCancelled,
    ProviderBookingCompleted,
    ProviderVerificationApproved,
    ProviderVerificationRejected,
    ProviderVerificationDocumentDownloaded,
    ProviderServicePublished,
    ProviderServicePaused,
    ProviderServiceArchived,
    ProviderBookingRescheduled,
    ProviderPaymentReported,
    ProviderPaymentConfirmed,
    ProviderPaymentDisputed,
    ProviderPaymentRefunded,
    ProviderIncidentOpened,
    ProviderIncidentInvestigationStarted,
    ProviderIncidentResolved,
    ProviderIncidentAppealed,
    ProviderIncidentClosed,
    ServiceProviderMembershipChanged,

    // Regulatory exports
    RegulatoryExportRequested,
    RegulatoryExportStarted,
    RegulatoryExportCompleted,
    RegulatoryExportFailed,
    RegulatoryExportDownloaded,
    RegulatoryExportExpired,
    RegulatorySubmissionPrepared,
}

public sealed class AuditLogEntry
{
    private AuditLogEntry() { } // EF Core

    public Guid Id { get; private set; }
    public Guid AdminUserId { get; private set; }
    public AuditAction Action { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    /// <summary>Optional freeform context (e.g. rejection reason, tier name).</summary>
    public string? Details { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }

    public static AuditLogEntry Create(
        Guid adminUserId,
        AuditAction action,
        string entityType,
        string entityId,
        string? details = null) => new()
        {
            Id = Guid.CreateVersion7(),
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details?.Trim(),
            PerformedAt = DateTimeOffset.UtcNow,
        };
}
