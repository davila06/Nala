namespace PawTrack.Domain.Bundles;

public enum BundleOrderStatus
{
    PendingPayment = 0, // order created, awaiting SINPE transfer
    Paid = 1,           // admin confirmed payment received
    Sourcing = 2,       // collar being ordered/acquired on demand
    Shipped = 3,        // dispatched to customer with tracking
    Delivered = 4,      // customer received the collar
    Cancelled = 5,      // order cancelled (refund required if Paid+)
}
