namespace PawTrack.Domain.Bundles;

/// <summary>
/// Physical GPS collar hardware models available in bundle orders.
/// Legacy values (TractiveGPSDog4/Cat4) are preserved for database backwards compatibility,
/// while new orders map to PawTrack's official hardware lineup.
/// </summary>
public enum CollarModel
{
    TractiveGPSDog4 = 1, // Legacy bundle collar — perros, IPX7
    TractiveGPSCat4 = 2, // Legacy bundle collar — gatos, ultra liviano
    PawTrackAL600 = 3,   // PawTrack AL600 GPS inteligente (versión Latinoamérica)
}
