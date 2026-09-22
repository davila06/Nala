namespace PawTrack.Application.Subscriptions.Services;

public sealed record AddonProrationQuote(
    decimal FullTermPriceCrc,
    decimal UnusedCreditCrc,
    decimal NewTermPriceCrc,
    decimal AmountDueCrc);

public static class SubscriptionAddonProration
{
    public static AddonProrationQuote Quote(
        decimal fullTermPriceCrc,
        DateTimeOffset currentStartsAt,
        DateTimeOffset currentExpiresAt,
        DateTimeOffset changeAt,
        decimal newTermPriceCrc)
    {
        if (fullTermPriceCrc < 0 || newTermPriceCrc < 0)
            throw new ArgumentOutOfRangeException(nameof(fullTermPriceCrc));
        if (currentExpiresAt <= currentStartsAt || changeAt < currentStartsAt)
            throw new ArgumentException("Invalid addon term or change date.");

        var totalDays = (currentExpiresAt - currentStartsAt).TotalDays;
        var unusedDays = Math.Max(0, (currentExpiresAt - changeAt).TotalDays);
        var credit = Math.Round(fullTermPriceCrc * (decimal)(unusedDays / totalDays), 2, MidpointRounding.AwayFromZero);
        var due = Math.Max(0m, newTermPriceCrc - credit);
        return new AddonProrationQuote(fullTermPriceCrc, credit, newTermPriceCrc, due);
    }
}
