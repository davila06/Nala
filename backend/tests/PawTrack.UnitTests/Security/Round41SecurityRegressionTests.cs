using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.API.Middleware;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-41 security regression tests.
///
/// Gap: <c>ExceptionHandlingMiddleware</c> returns <c>ex.GetType().Name</c> and
/// <c>ex.Message</c> in the HTTP response body when
/// <c>IWebHostEnvironment.IsDevelopment()</c> is true:
///
///   <code>
///   var detail = env.IsDevelopment() ? $"{ex.GetType().Name}: {ex.Message}" : null;
///   </code>
///
/// If a staging or preview environment is tagged as <c>Development</c>
/// (a common misconfiguration in Azure App Service / GitHub Actions), unhandled
/// exceptions — including SQL Server errors from EF Core — are returned verbatim
/// to the caller. EF Core exception messages can contain:
///   • Table and column names
///   • Constraint names (e.g. <c>UQ_Users_Email</c>)
///   • Fragments of the SQL statement that failed
///
/// This constitutes OWASP A05 Security Misconfiguration: information disclosure
/// that should only appear in structured logs (Application Insights), never in
/// the HTTP response regardless of environment.
///
/// Fix:
///   Remove the environment-conditional branch entirely.
///   Always pass <c>null</c> as <c>detail</c>; route diagnostics to the logger
///   (already done via <c>LogError</c>) and Application Insights.
/// </summary>
public sealed class Round41SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ExceptionHandlingMiddleware BuildMiddleware(bool isDevelopment, RequestDelegate next)
    {
        // isDevelopment param kept for test readability (documents the scenario);
        // the middleware no longer uses IWebHostEnvironment after Round-41 fix.
        _ = isDevelopment;

        return new ExceptionHandlingMiddleware(
            next,
            NullLogger<ExceptionHandlingMiddleware>.Instance);
    }

    private static DefaultHttpContext BuildContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExceptionHandlingMiddleware_WhenDevelopment_DoesNotLeakExceptionMessage()
    {
        // Arrange — middleware wired with IsDevelopment() = true
        var exMessage = "SqlException: Invalid column name 'users_secret_column'";
        var middleware = BuildMiddleware(isDevelopment: true, next: _ =>
            throw new InvalidOperationException(exMessage));

        var ctx = BuildContext();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert — read the response body
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        body.Should().NotContain(exMessage,
            "exception messages can contain SQL schema details and must never " +
            "be returned to the HTTP caller, even in Development environments.");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_WhenDevelopment_DoesNotLeakExceptionTypeName()
    {
        // Arrange
        var middleware = BuildMiddleware(isDevelopment: true, next: _ =>
            throw new InvalidOperationException("some internal message"));

        var ctx = BuildContext();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        body.Should().NotContain("InvalidOperationException",
            "the exception type name is an internal implementation detail; " +
            "returning it helps attackers fingerprint the server stack.");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_WhenProduction_DoesNotLeakExceptionMessage()
    {
        // Arrange — baseline: production behaviour must be clean too
        var middleware = BuildMiddleware(isDevelopment: false, next: _ =>
            throw new InvalidOperationException("top-secret internal reason"));

        var ctx = BuildContext();

        // Act
        await middleware.InvokeAsync(ctx);

        // Assert
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();

        body.Should().NotContain("top-secret internal reason",
            "exception detail must never appear in production responses.");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_Always_Returns500WithGenericTitle()
    {
        // Arrange — any environment, any exception — generic problem details only
        foreach (var isDev in new[] { true, false })
        {
            var middleware = BuildMiddleware(isDev, next: _ =>
                throw new Exception("anything"));

            var ctx = BuildContext();

            // Act
            await middleware.InvokeAsync(ctx);

            // Assert — status code
            ctx.Response.StatusCode.Should().Be(500,
                "unhandled exceptions must always yield 500, regardless of environment.");

            // Assert — generic title present
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
            body.Should().Contain("unexpected",
                "the response must contain the generic 'unexpected error' message.");
        }
    }
}
