namespace PawTrack.Domain.Municipalities;

public enum MunicipalTier
{
    Basica = 0,       // Portal básico — solo cantón propio, sin foto, sin estadísticas
    Full = 1,         // Todo Básica + fotos, estadísticas de cantón, búsqueda multi-cantón, API
    RedRegional = 2,  // Todo Full + multi-cantón, transferencias, dashboard regional
}
