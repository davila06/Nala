using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Payments;

namespace PawTrack.UnitTests.Payments.Services;

public sealed class ElectronicBillingServiceTests
{
    private readonly IElectronicInvoiceRepository _invoiceRepo = Substitute.For<IElectronicInvoiceRepository>();
    private readonly IUserBillingProfileRepository _billingProfileRepo = Substitute.For<IUserBillingProfileRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    private ElectronicBillingService CreateSut() =>
        new(_invoiceRepo, _billingProfileRepo, _userRepo, _blobStorage, _unitOfWork, _config, NullLogger<ElectronicBillingService>.Instance);

    [Fact]
    public async Task EmitInvoice_WhenConsumerWithoutProfile_EmitsTiqueteElectronico()
    {
        var (user, _) = User.Create("cliente@pawtrack.cr", "hash", "Maria Rodriguez");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _billingProfileRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns((UserBillingProfile?)null);
        _invoiceRepo.GetNextSequenceNumberAsync(ElectronicInvoiceDocumentType.TiqueteElectronico, Arg.Any<CancellationToken>())
            .Returns("0000000001");
        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.blob.core.windows.net/invoices/test.pdf");

        var sut = CreateSut();
        var result = await sut.EmitInvoiceForTransactionAsync(
            new EmitInvoiceRequest(user.Id, 2990m, "Suscripción Plus", CabysCatalog.SoftwareSubscriptionCabys),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocumentType.Should().Be(ElectronicInvoiceDocumentType.TiqueteElectronico.ToString());
        result.Value.ClaveNumerica.Should().HaveLength(50);
        result.Value.NumeroConsecutivo.Should().StartWith("0010000104");
        result.Value.TotalAmountCrc.Should().Be(2990m);
        result.Value.SubtotalCrc.Should().Be(2646.02m);
        result.Value.IvaAmountCrc.Should().Be(343.98m);

        await _invoiceRepo.Received(1).AddAsync(Arg.Any<ElectronicInvoice>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmitInvoice_WhenProfileRequiresInvoice_EmitsFacturaElectronicaWithTaxId()
    {
        var (user, _) = User.Create("empresa@veterinaria.cr", "hash", "Clinica Central");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var profile = UserBillingProfile.Create(
            user.Id, TaxIdentificationType.Juridica, "3101888999", "Clinica Central S.A.", "facturas@veterinaria.cr", requiresInvoice: true);
        _billingProfileRepo.GetByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(profile);

        _invoiceRepo.GetNextSequenceNumberAsync(ElectronicInvoiceDocumentType.FacturaElectronica, Arg.Any<CancellationToken>())
            .Returns("0000000005");
        _blobStorage.UploadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.blob.core.windows.net/invoices/factura.pdf");

        var sut = CreateSut();
        var result = await sut.EmitInvoiceForTransactionAsync(
            new EmitInvoiceRequest(user.Id, 35000m, "Plan Clinica Partner", CabysCatalog.SoftwareSubscriptionCabys),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocumentType.Should().Be(ElectronicInvoiceDocumentType.FacturaElectronica.ToString());
        result.Value.NumeroConsecutivo.Should().StartWith("0010000101");
        result.Value.ReceiverName.Should().Be("CLINICA CENTRAL S.A.");
        result.Value.ReceiverIdNumber.Should().Be("3101888999");
    }

    [Fact]
    public async Task GenerateInvoicePdf_ProducesValidPdfBytes()
    {
        var invoice = ElectronicInvoice.Create(
            Guid.NewGuid(),
            ElectronicInvoiceDocumentType.FacturaElectronica,
            "506110926000310199999900100001010000000001199999999",
            "00100001010000000001",
            CabysCatalog.SoftwareSubscriptionCabys,
            "Suscripción Plus Mensual",
            2990m,
            "Juan Perez",
            "juan@test.cr");

        _invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var sut = CreateSut();
        var pdfBytes = await sut.GenerateInvoicePdfAsync(invoice.Id, CancellationToken.None);

        pdfBytes.Should().NotBeNullOrEmpty();
        // PDF magic bytes %PDF
        pdfBytes.Take(4).Should().BeEquivalentTo(new byte[] { 0x25, 0x50, 0x44, 0x46 });
    }
}
