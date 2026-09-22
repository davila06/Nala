using FluentAssertions;
using PawTrack.Application.Subscriptions.Services;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class SubscriptionAddonProrationTests
{
    [Fact]
    public void Quote_credits_unused_term_and_never_goes_negative()
    {
        var starts = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var expires = starts.AddDays(30);
        var quote = SubscriptionAddonProration.Quote(3000m, starts, expires, starts.AddDays(15), 2000m);

        quote.UnusedCreditCrc.Should().Be(1500m);
        quote.AmountDueCrc.Should().Be(500m);
    }
}
