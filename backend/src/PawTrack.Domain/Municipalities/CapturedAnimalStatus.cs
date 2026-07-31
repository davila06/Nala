namespace PawTrack.Domain.Municipalities;

public enum CapturedAnimalStatus
{
    Received   = 0,  // animal ingresado al refugio/perrera municipal
    OwnerFound = 1,  // dueño localizado vía PawTrack o búsqueda manual
    Transferred = 2, // transferido a otro refugio o municipalidad
    Released   = 3,  // liberado (animal salvaje, no mascota)
    Adopted    = 4,
}
