namespace PawTrack.Domain.ServiceProviders;

public enum ProviderBookingStatus
{
    Requested,
    Confirmed,
    InProgress,
    Completed,
    CancelledByCustomer,
    CancelledByProvider,
    NoShow,
    Expired,
    AwaitingPayment,
    Disputed,
    Refunded,
}