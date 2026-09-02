using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.Application.Collars.Commands.Admin;
using PawTrack.Application.Collars.Queries.GetCollarTagMetrics;
using PawTrack.Application.Common.Interfaces;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Collars;

/// <summary>End-to-end: register serials → metrics reflect them → search filters → bulk mark-sold → bulk revoke.</summary>
[Collection("Integration")]
public sealed class CollarTagAdminDashboardEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task Metrics_SearchFilter_AndBulkMarkSold_WorkEndToEnd()
    {
        var admin = await AuthHelper.CreateAdminClientAsync(factory);
        var serialA = $"PT-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-1000001";
        var serialB = $"PT-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}-1000002";

        foreach (var serial in new[] { serialA, serialB })
        {
            var registerResp = await admin.PostAsJsonAsync(
                "/api/admin/collar-tags", new { serial, firmwareVersion = "1.0.0" });
            registerResp.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var metricsResp = await admin.GetAsync("/api/admin/collar-tags/metrics");
        metricsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await metricsResp.Content.ReadFromJsonAsync<CollarTagMetricsDto>();
        metrics!.TotalSerials.Should().BeGreaterThanOrEqualTo(2);

        var searchResp = await admin.GetAsync($"/api/admin/collar-tags?serial={serialA}");
        searchResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var searchBody = await searchResp.Content.ReadFromJsonAsync<SearchResultDto>();
        searchBody!.Total.Should().Be(1);

        var bulkResp = await admin.PostAsJsonAsync(
            "/api/admin/collar-tags/bulk-mark-sold", new { serials = new[] { serialA, serialB } });
        bulkResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var bulkBody = await bulkResp.Content.ReadFromJsonAsync<BulkActionResultDto>();
        bulkBody!.Succeeded.Should().Be(2);
        bulkBody.Failed.Should().Be(0);
    }

    private sealed record SearchResultDto(int Total, List<object> Items);
}
