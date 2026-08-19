namespace PawTrack.Domain.Stores;

public enum StoreOrderStatus
{
    PendingPayment = 0, // customer placed order, awaiting SINPE
    PaymentReported = 1, // customer reported payment
    Confirmed = 2, // store confirmed + payment verified
    Preparing = 3, // store is preparing the order
    ReadyForPickup = 4, // ready — customer can collect
    OutForDelivery = 5, // on the way (delivery orders)
    Delivered = 6, // completed
    Cancelled = 7, // cancelled by store or customer
}
