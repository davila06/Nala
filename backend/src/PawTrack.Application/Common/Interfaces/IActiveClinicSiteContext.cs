namespace PawTrack.Application.Common.Interfaces;

public interface IActiveClinicSiteContext
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
    Guid? ClinicId { get; }
    bool IsClinicPrincipal { get; }
    bool IsPlatformAdministrator { get; }
    bool IsApiKeyPrincipal { get; }

    void Initialize(
        Guid? userId,
        Guid? sessionId,
        Guid? clinicId,
        bool isClinicPrincipal,
        bool isPlatformAdministrator,
        bool isApiKeyPrincipal);
}