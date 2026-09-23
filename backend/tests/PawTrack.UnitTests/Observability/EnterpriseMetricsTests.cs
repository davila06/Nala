using System.Diagnostics.Metrics;
using FluentAssertions;
using PawTrack.Infrastructure.Observability;

namespace PawTrack.UnitTests.Observability;

public sealed class EnterpriseMetricsTests
{
    [Fact]
    public void ExposesProductAndNorthStarInstruments()
    {
        var instruments = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) =>
        {
            if (instrument.Meter.Name == "PawTrack.Enterprise")
                instruments.Add(instrument.Name);
        };
        listener.Start();

        _ = EnterpriseMetrics.ProductEventsIngested;
        _ = EnterpriseMetrics.ProductFunnelQueries;
        _ = EnterpriseMetrics.NorthStarActiveProtectedPets;

        instruments.Should().Contain([
            "pawtrack.product.events.ingested",
            "pawtrack.product.funnel.queries",
            "pawtrack.north_star.active_protected_pets",
        ]);
    }
}
