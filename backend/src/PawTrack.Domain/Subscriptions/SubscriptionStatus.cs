namespace PawTrack.Domain.Subscriptions;

public enum SubscriptionStatus
{
    /// <summary>Payment reference generated; awaiting confirmation from payment provider.</summary>
    PendingPayment = 0,
    Active = 1,
    Cancelled = 2,
    Expired = 3,
}
