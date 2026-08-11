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
}
