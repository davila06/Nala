using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace PawTrack.API.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation failure: {Errors}", string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)));
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity,
                "Validation Error", ex.Errors.Select(e => e.ErrorMessage));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            // Never expose exception details (type name, message) in the HTTP response.
            // EF Core messages can contain table names, column names, and constraint names.
            // All diagnostic detail is captured by the logger / Application Insights above.
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        IEnumerable<string>? errors = null,
        string? detail = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        await context.Response.WriteAsync(json);
    }
}
