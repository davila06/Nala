using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.API.Middleware;
using PawTrack.Application.Collars;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarDeviceKeyMiddlewareTests
{
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly CollarDeviceKeyMiddleware _sut;
    private bool _nextCalled;

    public CollarDeviceKeyMiddlewareTests()
    {
        _sut = new CollarDeviceKeyMiddleware(
            _ => { _nextCalled = true; return Task.CompletedTask; },
            NullLogger<CollarDeviceKeyMiddleware>.Instance);
    }

    private HttpContext MakeContext(string? headerValue)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        if (headerValue is not null)
            ctx.Request.Headers["X-Collar-Key"] = headerValue;
        return ctx;
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_CallsNextWithoutAuthentication()
    {
        var ctx = MakeContext(null);

        await _sut.InvokeAsync(ctx, _credRepo, _uow);

        _nextCalled.Should().BeTrue();
        ctx.User.Identity?.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ValidKey_InjectsCollarIdClaimAndCallsNext()
    {
        var collarId = Guid.NewGuid();
        const string rawKey = "ptwk_collar_testkey";
        var hash = CollarDeviceKeyHasher.Compute(rawKey);
        var cred = CollarDeviceCredential.Create(collarId, hash);

        _credRepo.GetActiveByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(cred);
        var ctx = MakeContext(rawKey);

        await _sut.InvokeAsync(ctx, _credRepo, _uow);

        _nextCalled.Should().BeTrue();
        ctx.User.FindFirst("CollarId")?.Value.Should().Be(collarId.ToString());
    }

    [Fact]
    public async Task InvokeAsync_RevokedKey_Returns401AndDoesNotCallNext()
    {
        const string rawKey = "ptwk_collar_revokedkey";
        var hash = CollarDeviceKeyHasher.Compute(rawKey);
        _credRepo.GetActiveByHashAsync(hash, Arg.Any<CancellationToken>()).Returns((CollarDeviceCredential?)null);

        var ctx = MakeContext(rawKey);

        await _sut.InvokeAsync(ctx, _credRepo, _uow);

        _nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be(401);
    }
}
