namespace PawTrack.Domain.Notifications;

public enum NotificationType
{
    LostPetAlert,
    PetReunited,
    SightingAlert,
    ResolveCheck,
    StaleReportReminder,
    VerifiedAllyAlert,
    ChatMessage,
    FraudAlert,
    SystemMessage,
    FoundPetMatch,
    PreventiveAlert,
    CustodyStarted,
    CustodyClosed,
    NeighborLostPetAlert, // neighbor-network ultra-local alert (~500m radius)
    ActivityStreak,       // gamification streak milestone
    AdoptionInterest,     // shelter receives: someone applied to adopt
    AdoptionApproved,     // applicant receives: application approved
    AdoptionRejected,     // applicant receives: application rejected
    AdoptionFairAlert,    // nearby users: adoption fair in the area
    CollarOfflineAlert,   // collar stopped reporting past the configured threshold
    CollarLowBatteryAlert, // collar battery dropped below the configured threshold
    CollarSafeZoneBreach, // collar exited a defined safe zone
}
