namespace PawTrack.Application.Common.Interfaces;

public interface IFraudReportRepository
{
    Task AddAsync(Domain.Safety.FraudReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of fraud reports that target <paramref name="targetUserId"/>
    /// within the specified rolling <paramref name="window"/>.
    /// </summary>
    Task<int> CountRecentByTargetUserAsync(
        Guid       targetUserId,
        TimeSpan   window,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the number of anonymous fraud reports submitted from the same hashed IP
    /// within the specified rolling <paramref name="window"/>.
    /// </summary>
    Task<int> CountRecentByIpHashAsync(
        string    ipHash,
        TimeSpan  window,
        CancellationToken cancellationToken = default);
}
