using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PawTrack.API;
using System.Net;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-44 security regression tests.
///
/// Gap: All rate limiters in <c>Program.cs</c> use <c>AddFixedWindowLimiter</c>,
/// which creates a single <em>global</em> counter shared across every client.
///
/// Example — "login" policy configured as 5 req/60 s:
///   <code>
///   options.AddFixedWindowLimiter("login", o =>
///   {
///       o.PermitLimit = 5;
///       o.Window = TimeSpan.FromSeconds(60);
///   });
///   </code>
///
/// Consequences:
/// <list type="number">
///   <item>
///     <b>DoS via exhaustion</b> — an attacker making 5 <c>POST /api/auth/login</c>
///     requests per minute blocks <em>all users worldwide</em> from logging in
///     for the remainder of that window.
///   </item>
///   <item>
///     <b>Brute-force protection is illusory</b> — a single IP is not independently
///     constrained; it competes in the same global pool. A distributed attacker that
///     uses multiple IPs can exhaust the global pool without any single IP ever
///     being limited.
///   </item>
/// </list>
///
/// Fix:
///   Replace every <c>AddFixedWindowLimiter</c> with <c>AddPolicy</c> +
///   <c>RateLimitPartition.GetFixedWindowLimiter</c> partitioned on the client IP.
///   Extract the partition-key logic into <c>RateLimiterIpKey.Get(HttpContext)</c>
///   so it can be tested independently of the rate-limiter middleware.
/// </summary>
public sealed class Round44SecurityRegressionTests
{
    [Fact]
    public void RateLimiterIpKey_DifferentRemoteAddresses_ProduceDifferentKeys()
    {
        // Arrange — two clients with distinct IPs  
        var ctx1 = new DefaultHttpContext();
        ctx1.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.1"); // TEST-NET-3 (RFC 5737)

        var ctx2 = new DefaultHttpContext();
        ctx2.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.2");

        // Act
        var key1 = RateLimiterIpKey.Get(ctx1);
        var key2 = RateLimiterIpKey.Get(ctx2);

        // Assert — per-IP partitioning means each IP gets its own independent quota
        key1.Should().NotBe(key2,
            "per-IP partitioning requires that distinct IPs yield distinct partition keys " +
            "so one IP exhausting its quota does not block other clients");
    }

    [Fact]
    public void RateLimiterIpKey_NullRemoteAddress_ReturnsAnonymousKey()
    {
        // Arrange — HttpContext with no remote IP (e.g., test environment, Unix socket)
        var ctx = new DefaultHttpContext();
        // ctx.Connection.RemoteIpAddress is null by default

        // Act
        var key = RateLimiterIpKey.Get(ctx);

        // Assert — falls back to a single "anonymous" bucket rather than crashing
        key.Should().Be("anonymous",
            "unresolvable IPs must fall into a single shared bucket to avoid null keying");
    }

    [Fact]
    public void RateLimiterIpKey_SameIpInTwoContexts_ProducesSameKey()
    {
        // Arrange
        var ip = IPAddress.Parse("10.0.0.55");
        var ctx1 = new DefaultHttpContext();
        ctx1.Connection.RemoteIpAddress = ip;

        var ctx2 = new DefaultHttpContext();
        ctx2.Connection.RemoteIpAddress = ip;

        // Act
        var key1 = RateLimiterIpKey.Get(ctx1);
        var key2 = RateLimiterIpKey.Get(ctx2);

        // Assert — deterministic: same IP always maps to the same partition
        key1.Should().Be(key2,
            "the same IP address must always produce the same partition key " +
            "so that rate-limit counts accumulate correctly across requests");
    }
}
