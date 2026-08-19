namespace PawTrack.Domain.Stores;

public enum StoreStatus
{
    Pending = 0, // awaiting admin review
    Active = 1,
    Suspended = 2,
}
