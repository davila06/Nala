namespace PawTrack.Domain.Collars;

public enum CollarAuditEvent
{
    SerialRegistered,
    SerialMarkedSold,
    Activated,
    Deactivated,
    DeviceKeyRevoked,
    DeviceKeyRegenerated,
    LocationIngestFailed,
    HandoverCodeGenerated,
    HandoverCompleted,
    HandoverCancelled,
    LostModeActivated,
    LostModeDeactivated,
}
