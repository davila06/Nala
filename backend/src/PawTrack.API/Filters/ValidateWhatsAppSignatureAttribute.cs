using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PawTrack.API.Filters;

/// <summary>
/// Validates the HMAC-SHA256 signature that Meta Cloud API sends on every webhook POST.
/// <para>
/// Meta includes the header <c>X-Hub-Signature-256: sha256={hex_signature}</c>.
/// The signature is computed over the raw request body using the app secret as the HMAC key.
/// </para>
/// <para>
/// Requests that fail validation are rejected with HTTP 403 before reaching the controller.
/// GET requests (webhook verification handshake) are always allowed through.
/// </para>
/// <para>
/// Security: if <c>WhatsApp:AppSecret</c> is absent in production the request is
/// rejected with HTTP 500 to prevent a misconfigured deploy from silently opening
/// the webhook to unauthenticated traffic.  In development the check is downgraded
/// to a warning so local dry-run testing remains possible.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ValidateWhatsAppSignatureAttribute : TypeFilterAttribute
{
    public ValidateWhatsAppSignatureAttribute() : base(typeof(ValidateWhatsAppSignatureFilter)) { }
}

internal sealed class ValidateWhatsAppSignatureFilter(
    IConfiguration configuration,
    IWebHostEnvironment env,
    ILogger<ValidateWhatsAppSignatureFilter> logger)
    : IAsyncActionFilter
{
    private const string SignatureHeader = "X-Hub-Signature-256";
    private const string SignaturePrefix = "sha256=";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // GET requests are the Meta webhook verification handshake — no body, no signature
        if (HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            await next();
            return;
        }

        var appSecret = configuration["WhatsApp:AppSecret"];
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            if (env.IsDevelopment())
            {
                // Development dry-run: allow through but warn loudly
                logger.LogWarning(
                    "WhatsApp:AppSecret not configured — signature validation skipped (development dry-run mode). " +
                    "This MUST be set in production.");
                await next();
                return;
            }

            // Production / Staging: reject with 500 and emit a critical alert.
            // A missing secret in prod means the webhook is wide open — this is a
            // configuration failure that should page on-call immediately.
            logger.LogCritical(
                "SECURITY: WhatsApp:AppSecret is not configured in a non-development environment. " +
                "The webhook endpoint is UNSAFE. Returning 500 to prevent unauthenticated access.");

            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(SignatureHeader, out var headerValue)
            || string.IsNullOrWhiteSpace(headerValue))
        {
            logger.LogWarning("WhatsApp webhook request missing {Header}", SignatureHeader);
            context.Result = new ForbidResult();
            return;
        }

        var signature = headerValue.ToString();
        if (!signature.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("WhatsApp webhook signature has unexpected prefix: {Sig}", signature);
            context.Result = new ForbidResult();
            return;
        }

        // The request body was already buffered by EnableBuffering() in middleware
        context.HttpContext.Request.Body.Position = 0;
        var body = await new StreamReader(context.HttpContext.Request.Body).ReadToEndAsync();
        context.HttpContext.Request.Body.Position = 0;

        var keyBytes  = Encoding.UTF8.GetBytes(appSecret);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var expected  = $"{SignaturePrefix}{Convert.ToHexStringLower(HMACSHA256.HashData(keyBytes, bodyBytes))}";

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature)))
        {
            logger.LogWarning("WhatsApp webhook signature mismatch — request rejected.");
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
