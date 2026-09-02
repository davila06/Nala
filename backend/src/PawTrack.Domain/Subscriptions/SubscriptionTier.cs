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

    // Shelter / adoption tiers
    ShelterBasic = 300, // free — directory + up to 5 active animals
    ShelterPlus = 310, // ₡8,000/mes — unlimited animals + fairs + featured map pin

    // Municipality B2G tiers (annual billing)
    MuniBasica = 400, // ₡150,000/año — portal básico, un cantón
    MuniFull = 410,   // ₡300,000/año — fotos, estadísticas, API
    MuniRedRegional = 420, // ₡500,000/año — multi-cantón, dashboard regional
}
