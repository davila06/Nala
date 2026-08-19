namespace PawTrack.Domain.Subscriptions;

public enum SubscriptionTier
{
    // Pet-owner tiers
    Free = 0,
    UserPlus = 10,
    UserFamilia = 20,

    // Clinic tiers
    ClinicBasic = 100,
    ClinicPlus = 110,
    ClinicPartner = 120,

    // Store tiers
    StoreBasic = 200, // free — listed in directory + map
    StorePlus = 210, // ₡12,000/mes — featured, catalog, orders
    StorePartner = 220, // ₡25,000/mes — analytics, multi-location, badge
}
