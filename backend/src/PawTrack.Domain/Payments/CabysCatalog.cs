using PawTrack.Domain.Bundles;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Domain.Payments;

/// <summary>
/// Código de Clasificación de Bienes y Servicios (CABYS) emitido por el Banco Central de Costa Rica (BCCR)
/// y el Ministerio de Hacienda para facturación electrónica en Costa Rica.
/// </summary>
public static class CabysCatalog
{
    // Servicios de software, plataformas en la nube y suscripciones
    public const string SoftwareSubscriptionCabys = "8314100000000"; // Servicios de diseño y desarrollo de aplicaciones de tecnologías de la información
    public const string PetIdentificationSoftwareCabys = "8314300000000"; // Servicios de provisión de infraestructura y aplicaciones de TI en la nube (SaaS)

    // Hardware, collares GPS, placas y combos NFC
    public const string GpsHardwareTrackerCabys = "4525000000000"; // Receptores de radionavegación por satélite (GPS)
    public const string MetalQrTagCabys = "4299900000000"; // Placas y artículos identificatorios metálicos manufacturados
    public const string SiliconeNfcTagCabys = "3699000000000"; // Artículos manufacturados diversos de plástico y silicona con chip

    // Impuesto sobre el Valor Agregado (Costa Rica Ley 9635)
    public const decimal StandardIvaRate = 0.13m; // 13% IVA general

    public static string GetCabysForSubscription(SubscriptionTier tier) =>
        PetIdentificationSoftwareCabys;

    public static string GetCabysForBundleProduct(BundleProductType product) => product switch
    {
        BundleProductType.CollarGpsPlus or BundleProductType.CollarTagGps => GpsHardwareTrackerCabys,
        BundleProductType.QrPlate or BundleProductType.EmergencyPack => MetalQrTagCabys,
        BundleProductType.SiliconeTag or BundleProductType.NfcQrCombo => SiliconeNfcTagCabys,
        _ => GpsHardwareTrackerCabys,
    };
}

/// <summary>
/// Estructura de cabecera y desglose para Facturación Electrónica versión 4.3 (DGT Ministerio de Hacienda Costa Rica).
/// </summary>
public sealed record ElectronicInvoiceDetails(
    string ClaveNumerica50Digitos,
    string NumeroConsecutivo,
    DateTimeOffset FechaEmision,
    string EmisorNombre,
    string EmisorCedulaJuridica,
    string ReceptorNombre,
    string? ReceptorIdentificacion,
    string ReceptorEmail,
    string CodigoCabys,
    string DescripcionServicio,
    decimal MontoNeto,
    decimal TarifaIva,
    decimal MontoIva,
    decimal MontoTotal,
    string MedioPago = "02" // 02 = Tarjeta, 04 = Transferencia SINPE
);
