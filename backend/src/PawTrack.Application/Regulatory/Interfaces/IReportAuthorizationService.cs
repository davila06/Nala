using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public interface IReportAuthorizationService
{
    Task<ReportAuthorizationResult> AuthorizeAsync(
        Guid userId,
        ExportScope scope,
        string? canton,
        Guid? organizationId,
        ReportType reportType,
        CancellationToken cancellationToken = default);
}

public sealed record ReportAuthorizationResult(
    bool IsAuthorized,
    string? Error,
    string? EffectiveCanton,
    Guid? EffectiveOrganizationId)
{
    public static ReportAuthorizationResult Allow(string? canton, Guid? organizationId) =>
        new(true, null, canton, organizationId);

    public static ReportAuthorizationResult Deny(string error) =>
        new(false, error, null, null);
}
