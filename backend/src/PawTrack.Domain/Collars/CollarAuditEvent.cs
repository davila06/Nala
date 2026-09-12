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
    HeartbeatFailed,
    HandoverCodeGenerated,
    HandoverCompleted,
    HandoverCancelled,
    LostModeActivated,
    LostModeDeactivated,
}
