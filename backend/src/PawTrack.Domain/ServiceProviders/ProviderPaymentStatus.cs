namespace PawTrack.Domain.ServiceProviders;

public enum ProviderPaymentStatus
{
    Pending,
    Reported,
    Confirmed,
    Failed,
    Disputed,
    Refunded,
}