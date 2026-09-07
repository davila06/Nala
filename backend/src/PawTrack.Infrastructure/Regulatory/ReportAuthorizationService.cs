using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class ReportAuthorizationService(
    IMunicipalSubscriptionService municipalSubscriptionService)
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

        var authorizedCantons = await municipalSubscriptionService.GetAuthorizedCantonsAsync(userId, cancellationToken);
        if (authorizedCantons.Count > 0)
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

        return ReportAuthorizationResult.Allow(canton, organizationId);
    }
}
