using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;
using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.Queries.GetTaxSummary;

public sealed record CabysSalesBreakdownDto(
    string CodigoCabys,
    string Descripcion,
    decimal SubtotalCrc,
    decimal IvaAmountCrc,
    decimal TotalCrc,
    int DocumentCount);

public sealed record TaxSummaryDto(
    int Year,
    int Month,
    decimal TotalVentasCrc,
    decimal BaseImponibleGravadaCrc,
    decimal DebitoFiscalIva13Crc,
    int TotalComprobantesEmitidos,
    int FacturasElectronicasCount,
    int TiquetesElectronicosCount,
    IReadOnlyList<CabysSalesBreakdownDto> CabysBreakdown);

public sealed record GetTaxSummaryQuery(int Year, int Month, Guid RequestingUserId)
    : IRequest<Result<TaxSummaryDto>>;

public sealed class GetTaxSummaryQueryHandler(
    IUserRepository userRepository,
    IElectronicInvoiceRepository invoiceRepository)
    : IRequestHandler<GetTaxSummaryQuery, Result<TaxSummaryDto>>
{
    public async Task<Result<TaxSummaryDto>> Handle(
        GetTaxSummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.RequestingUserId, cancellationToken);
        if (user is null || user.Role != UserRole.Admin)
            return Result.Failure<TaxSummaryDto>("Acceso restringido a administradores.");

        var startDate = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = startDate.AddMonths(1);

        var invoices = await invoiceRepository.GetByPeriodAsync(startDate, endDate, cancellationToken);

        var totalVentas = invoices.Sum(x => x.TotalAmountCrc);
        var subtotal = invoices.Sum(x => x.SubtotalCrc);
        var iva = invoices.Sum(x => x.IvaAmountCrc);
        var facturasCount = invoices.Count(x => x.DocumentType == ElectronicInvoiceDocumentType.FacturaElectronica);
        var tiquetesCount = invoices.Count(x => x.DocumentType == ElectronicInvoiceDocumentType.TiqueteElectronico);

        var breakdown = invoices
            .GroupBy(x => x.CodigoCabys)
            .Select(g => new CabysSalesBreakdownDto(
                CodigoCabys: g.Key,
                Descripcion: DescribeCabys(g.Key),
                SubtotalCrc: g.Sum(x => x.SubtotalCrc),
                IvaAmountCrc: g.Sum(x => x.IvaAmountCrc),
                TotalCrc: g.Sum(x => x.TotalAmountCrc),
                DocumentCount: g.Count()))
            .OrderByDescending(x => x.TotalCrc)
            .ToList();

        return Result.Success(new TaxSummaryDto(
            Year: request.Year,
            Month: request.Month,
            TotalVentasCrc: totalVentas,
            BaseImponibleGravadaCrc: subtotal,
            DebitoFiscalIva13Crc: iva,
            TotalComprobantesEmitidos: invoices.Count,
            FacturasElectronicasCount: facturasCount,
            TiquetesElectronicosCount: tiquetesCount,
            CabysBreakdown: breakdown));
    }

    private static string DescribeCabys(string cabys) => cabys switch
    {
        CabysCatalog.SoftwareSubscriptionCabys or CabysCatalog.PetIdentificationSoftwareCabys =>
            "Servicios SaaS de Plataforma y Protección Animal",
        CabysCatalog.GpsHardwareTrackerCabys =>
            "Dispositivos y Collares GPS Inteligentes",
        CabysCatalog.MetalQrTagCabys =>
            "Placas Metálicas con Grabado Láser QR",
        CabysCatalog.SiliconeNfcTagCabys =>
            "Tags de Silicona con Chip NFC NTAG213",
        _ => "Otros bienes y servicios gravados",
    };
}
