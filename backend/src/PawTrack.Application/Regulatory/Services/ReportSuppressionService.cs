namespace PawTrack.Application.Regulatory.Services;

public sealed record ReportGroup(string Key, string Dimension, int Count);

public sealed record SuppressedReportGroup(string Key, string Dimension, int Count, bool IsSuppressed);

public sealed record SuppressionResult(
    IReadOnlyList<SuppressedReportGroup> Groups,
    int SuppressedRowCount);

public static class ReportSuppressionService
{
    public static SuppressionResult Apply(IEnumerable<ReportGroup> groups, int threshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 1);

        var result = groups.Select(group => group.Count < threshold
                ? new SuppressedReportGroup(group.Key, group.Dimension, 0, IsSuppressed: true)
                : new SuppressedReportGroup(group.Key, group.Dimension, group.Count, IsSuppressed: false))
            .ToList();

        return new SuppressionResult(
            result,
            result.Count(group => group.IsSuppressed));
    }
}
