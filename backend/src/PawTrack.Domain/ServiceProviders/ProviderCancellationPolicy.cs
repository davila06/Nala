namespace PawTrack.Domain.ServiceProviders;

public sealed record ProviderCancellationPolicy(
    string Code,
    int FreeCancellationHours,
    decimal CustomerRefundPercentage,
    decimal ProviderCancellationRefundPercentage)
{
    public static ProviderCancellationPolicy Standard { get; } =
        new("Standard", 48, 100, 100);

    public bool IsCustomerCancellationFree(DateTimeOffset startsAt, DateTimeOffset now) =>
        startsAt - now >= TimeSpan.FromHours(FreeCancellationHours);
}
