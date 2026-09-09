using System.Net;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.B2B;

[Collection("Integration")]
public sealed class B2BAuthorizationSmokeTests(PawTrackWebApplicationFactory factory)
{
    [Fact]
    public async Task OwnerCannotAccessStoreAnalyticsOrClinicApiKeys()
    {
        using var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var storeAnalytics = await client.GetAsync("/api/stores/me/analytics/export");
        var clinicApiKeys = await client.GetAsync("/api/clinics/me/api-keys");

        storeAnalytics.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        clinicApiKeys.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}