using PawTrack.Application.Common.Interfaces;

namespace PawTrack.API.Middleware;

public sealed class ActiveClinicSiteContext : IActiveClinicSiteContext
{
    public Guid? UserId { get; private set; }
    public Guid? SessionId { get; private set; }
    public Guid? ClinicId { get; private set; }
    public bool IsClinicPrincipal { get; private set; }
    public bool IsPlatformAdministrator { get; private set; }
    public bool IsApiKeyPrincipal { get; private set; }

    public void Initialize(
        Guid? userId,
        Guid? sessionId,
        Guid? clinicId,
        bool isClinicPrincipal,
        bool isPlatformAdministrator,
        bool isApiKeyPrincipal)
    {
        UserId = userId;
        SessionId = sessionId;
        ClinicId = clinicId;
        IsClinicPrincipal = isClinicPrincipal;
        IsPlatformAdministrator = isPlatformAdministrator;
        IsApiKeyPrincipal = isApiKeyPrincipal;
    }
}
