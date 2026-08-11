namespace PawTrack.Domain.Bundles;

public enum BundleProductType
{
    CollarGpsPlus   = 0, // GPS collar (Tractive) + 12 months Plus — original bundle
    QrPlate         = 1, // Aluminum QR plate only
    SiliconeTag     = 2, // Silicone QR tag
    NfcQrCombo      = 3, // NFC NTAG213 chip + QR plate — tap or scan
    EmergencyPack   = 4, // QR plate + wallet card emergency pack
}
