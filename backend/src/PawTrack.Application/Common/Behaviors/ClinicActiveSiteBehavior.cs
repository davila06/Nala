using MediatR;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Application.Common.Behaviors;

[AttributeUsage(AttributeTargets.Class)]
public sealed class BypassClinicActiveSiteAttribute : Attribute;

public sealed class ClinicActiveSiteRequiredException : Exception;

public sealed class ClinicActiveSiteBehavior<TRequest, TResponse>(IActiveClinicSiteContext siteContext)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var requestNamespace = requestType.Namespace ?? string.Empty;
        if (!siteContext.IsClinicPrincipal
            || siteContext.IsPlatformAdministrator
            || requestType.IsDefined(typeof(BypassClinicActiveSiteAttribute), inherit: true)
            || !IsClinicRequestNamespace(requestNamespace))
            return next();

        var clinicIdProperty = requestType.GetProperty("ClinicId");
        if (clinicIdProperty?.GetValue(request) is not Guid requestedClinicId || requestedClinicId == Guid.Empty)
            return next();

        if (siteContext.ClinicId != requestedClinicId)
            throw new ClinicActiveSiteRequiredException();

        return next();
    }

    private static bool IsClinicRequestNamespace(string requestNamespace) =>
        requestNamespace.StartsWith("PawTrack.Application.Clinics", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Certificates", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Medical.ClinicAccess", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.Pets.SanitaryIdentity", StringComparison.Ordinal)
        || requestNamespace.StartsWith("PawTrack.Application.CastrationCampaigns", StringComparison.Ordinal);
}
