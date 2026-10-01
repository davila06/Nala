using FluentAssertions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Domain;

public sealed class PaymentLedgerEntryDomainTests
{
    [Fact]
    public void Ledger_entry_is_created_with_an_immutable_financial_snapshot()
    {
        var entry = PaymentLedgerEntry.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentLedgerEntryType.Debit,
            4990.129m,
            "crc",
            "capture-1",
            "corr-1");

        entry.AmountCrc.Should().Be(4990.13m);
        entry.Currency.Should().Be("CRC");
        entry.EntryType.Should().Be(PaymentLedgerEntryType.Debit);
        entry.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }
}
