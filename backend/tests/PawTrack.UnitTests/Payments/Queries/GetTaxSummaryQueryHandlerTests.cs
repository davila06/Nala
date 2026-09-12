using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Payments.Queries.GetTaxSummary;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Queries;

public sealed class GetTaxSummaryQueryHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IElectronicInvoiceRepository _invoiceRepo = Substitute.For<IElectronicInvoiceRepository>();

    [Fact]
    public async Task Handle_WhenNonAdminUser_ReturnsFailure()
    {
        var (user, _) = User.Create("owner@pawtrack.cr", "hash", "Owner User");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sut = new GetTaxSummaryQueryHandler(_userRepo, _invoiceRepo);

        var result = await sut.Handle(new GetTaxSummaryQuery(2026, 9, user.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso restringido a administradores.");
    }

    [Fact]
    public async Task Handle_WhenAdminUser_ReturnsAccurateTaxTotalsAndCabysBreakdown()
    {
        var (user, _) = User.Create("admin@pawtrack.cr", "hash", "Admin User");
        user.PromoteToAdmin();
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var inv1 = ElectronicInvoice.Create(
            user.Id,
            ElectronicInvoiceDocumentType.FacturaElectronica,
            "506110926000310199999900100001010000000001199999991",
            "00100001010000000001",
            CabysCatalog.SoftwareSubscriptionCabys,
            "Plan Plus",
            2990m,
            "Cliente 1",
            "c1@test.cr");

        var inv2 = ElectronicInvoice.Create(
            user.Id,
            ElectronicInvoiceDocumentType.TiqueteElectronico,
            "506110926000310199999900100001040000000001199999992",
            "00100001040000000001",
            CabysCatalog.GpsHardwareTrackerCabys,
            "Collar GPS",
            49900m,
            "Cliente 2",
            "c2@test.cr");

        _invoiceRepo.GetByPeriodAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<ElectronicInvoice> { inv1, inv2 });

        var sut = new GetTaxSummaryQueryHandler(_userRepo, _invoiceRepo);
        var result = await sut.Handle(new GetTaxSummaryQuery(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalVentasCrc.Should().Be(2990m + 49900m);
        result.Value.TotalComprobantesEmitidos.Should().Be(2);
        result.Value.FacturasElectronicasCount.Should().Be(1);
        result.Value.TiquetesElectronicosCount.Should().Be(1);
        result.Value.CabysBreakdown.Should().HaveCount(2);
    }
}
