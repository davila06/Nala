using System.Diagnostics;
using PawTrack.Infrastructure.Observability;

namespace PawTrack.API.Middleware;

public sealed class EnterpriseRequestMetricsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var route = context.GetEndpoint()?.Metadata
                .GetMetadata<Microsoft.AspNetCore.Routing.RouteNameMetadata>()?.RouteName
                ?? context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                ?? "unknown";
            var status = context.Response.StatusCode;
            var tags = new TagList
            {
                { "method", context.Request.Method },
                { "route", route },
                { "status_class", $"{status / 100}xx" },
            };

            EnterpriseMetrics.ApiRequests.Add(1, tags);
            EnterpriseMetrics.ApiRequestDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
            if (status >= 500)
                EnterpriseMetrics.ApiErrors.Add(1, tags);
        }
    }
}
