namespace PawTrack.Domain.Promotions;

public enum PromotionType
{
    // stored as int — do NOT reorder or DB values break
    PercentageDiscount = 0, // DES10XXX / DES15XXX
    FreeTier           = 1, // FREEPLXX / FREEFAXX
    FreeMonths         = 2, // MES01XXX / MES03XXX / MES06XXX
}
