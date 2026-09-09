using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class ReportAuthorizationService(
    IMunicipalSubscriptionService municipalSubscriptionService,
    IMunicipalProfileRepository municipalProfileRepository,
    IUserRepository userRepository)
    : IReportAuthorizationService
{
    public async Task<ReportAuthorizationResult> AuthorizeAsync(
        Guid userId,
        ExportScope scope,
        string? canton,
        Guid? organizationId,
        ReportType reportType,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return ReportAuthorizationResult.Deny("Usuario requerido.");

        if (scope == ExportScope.Public)
            return ReportAuthorizationResult.Allow(null, null);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var isAdmin = user?.Role == Domain.Auth.UserRole.Admin;
        MunicipalityProfile? municipalProfile = null;

        if (scope is ExportScope.Admin or ExportScope.Nala && !isAdmin)
            return ReportAuthorizationResult.Deny("Este alcance requiere permisos de Admin.");

        if (scope == ExportScope.Institutional && !isAdmin)
        {
            municipalProfile = await municipalProfileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (municipalProfile is null)
                return ReportAuthorizationResult.Deny("El usuario no tiene un perfil municipal autorizado.");
            if (string.IsNullOrWhiteSpace(canton))
                return ReportAuthorizationResult.Deny("El cantón es requerido para este alcance.");
        }

        var authorizedCantons = await municipalSubscriptionService.GetAuthorizedCantonsAsync(userId, cancellationToken);
        if (scope == ExportScope.Institutional && !isAdmin)
        {
            if (authorizedCantons.Count == 0 || string.IsNullOrWhiteSpace(canton))
                return ReportAuthorizationResult.Deny("El usuario no tiene cantones autorizados para este alcance.");
            if (!authorizedCantons.Contains(canton, StringComparer.OrdinalIgnoreCase))
                return ReportAuthorizationResult.Deny("El usuario no tiene acceso al cantón solicitado.");
        }
        else if (authorizedCantons.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(canton))
                return ReportAuthorizationResult.Deny("El cantón es requerido para este alcance.");
            if (!authorizedCantons.Contains(canton, StringComparer.OrdinalIgnoreCase))
                return ReportAuthorizationResult.Deny("El usuario no tiene acceso al cantón solicitado.");
        }

        // OrganizationId is only accepted when the caller supplies an explicit scope.
        // The current MVP has no cross-organization delegation contract, so a caller
        // cannot request another organization by ID.
        if (organizationId.HasValue && organizationId.Value == Guid.Empty)
            return ReportAuthorizationResult.Deny("La organización no es válida.");

        if (organizationId.HasValue)
        {
            if (!isAdmin)
            {
                municipalProfile ??= await municipalProfileRepository.GetByUserIdAsync(userId, cancellationToken);
                if (municipalProfile is null || municipalProfile.Id != organizationId.Value)
                    return ReportAuthorizationResult.Deny("El usuario no tiene acceso a la organización solicitada.");
            }
        }

        return ReportAuthorizationResult.Allow(canton, organizationId);
    }
}
