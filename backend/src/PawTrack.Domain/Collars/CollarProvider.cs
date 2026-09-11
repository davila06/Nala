namespace PawTrack.Domain.Collars;

public enum CollarProvider
{
    Own = 0, // PawTrack hardware (future)
    Tractive = 1,
    [Obsolete("Kippy is not supported and has been deprecated.", false)]
    Kippy = 2,
    JimiTrackSolid = 3, // Jimi IoT AL600 via TrackSolid Pro API
    Generic = 99,
}
