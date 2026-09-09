using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PawTrack.API.Middleware;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Clinics;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicApiKeyMiddlewareTests
{
    [Fact]
    public async Task Invoke_ApiKeyCannotAccessApiKeyManagement()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");
        clinic.Activate();
        var rawKey = "ptwk_test-key";
        var key = ClinicApiKey.Create(clinic.Id, ClinicApiKeyHasher.Compute(rawKey), "M2M", scopes: [ClinicApiScope.Scan]);
        var context = BuildContext("/api/clinics/me/api-keys", rawKey);
        var nextCalled = false;
        var middleware = new ClinicApiKeyMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, Substitute.For<ILogger<ClinicApiKeyMiddleware>>());
        var keys = Substitute.For<IClinicApiKeyRepository>();
        keys.GetByHashAsync(ClinicApiKeyHasher.Compute(rawKey), Arg.Any<CancellationToken>()).Returns(key);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        await middleware.InvokeAsync(context, keys, clinics, Substitute.For<IUnitOfWork>());

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Invoke_ExportDownloadRequiresMedicalExportScope()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");
        clinic.Activate();
        var rawKey = "ptwk_test-key";
        var key = ClinicApiKey.Create(clinic.Id, ClinicApiKeyHasher.Compute(rawKey), "M2M", scopes: [ClinicApiScope.MedicalRead]);
        var context = BuildContext($"/api/clinics/medical-exports/{Guid.NewGuid()}/download", rawKey);
        var middleware = new ClinicApiKeyMiddleware(_ => Task.CompletedTask, Substitute.For<ILogger<ClinicApiKeyMiddleware>>());
        var keys = Substitute.For<IClinicApiKeyRepository>();
        keys.GetByHashAsync(ClinicApiKeyHasher.Compute(rawKey), Arg.Any<CancellationToken>()).Returns(key);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        await middleware.InvokeAsync(context, keys, clinics, Substitute.For<IUnitOfWork>());

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private static DefaultHttpContext BuildContext(string path, string rawKey)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers["X-PawTrack-Key"] = rawKey;
        return context;
    }
}
