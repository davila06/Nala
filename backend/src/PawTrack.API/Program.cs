using PawTrack.Infrastructure.Notifications;
using PawTrack.API;
using PawTrack.API.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Net;  // IPAddress
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using PawTrack.Application;
using PawTrack.Application.Sightings.VisualMatch;
using PawTrack.Infrastructure;
using PawTrack.API.Middleware;
using HO = Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ── Global request body size limit ────────────────────────────────────────────
// Kestrel's default is 30 MB — far too large for a JSON API.
// We cap at 1 MB globally; multipart/file-upload endpoints override this
// with their own [RequestSizeLimit(5_242_880)] attribute where needed.
// This prevents memory-exhaustion DoS from oversized JSON bodies on any
// endpoint that doesn't have an explicit per-action override.
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 1_048_576; // 1 MB
});

// ── Startup guards — fail fast on misconfiguration ────────────────────────────
// NOTE: guard is intentionally called after builder.Build() (see below) so that
// WebApplicationFactory can inject a safe test key + environment before the check.
// Production behaviour is identical: no factory is involved, so the key comes from
// Key Vault / environment variables as required.

// ── Application Insights ──────────────────────────────────────────────────────
builder.Services.AddApplicationInsightsTelemetry();

// ── Application + Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddSingleton(new VisualMatchSettings(
    builder.Configuration["VisualMatch:BaseUrl"] ?? "https://pawtrack.cr"));

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Forwarded Headers (Azure App Service / Front Door reverse-proxy) ─────────
// Must be configured BEFORE app.Build() so that UseForwardedHeaders() exposes
// the real client IP in HttpContext.Connection.RemoteIpAddress.  We restrict
// trust to RFC-1918 private ranges (the Azure infrastructure network) to
// prevent IP spoofing by external clients that craft X-Forwarded-For headers.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Clear default loopback-only trust; trust all RFC-1918 private ranges
    // used by Azure App Service / Front Door infra.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // RFC-1918 private ranges — matches Azure internal forwarding infrastructure
    options.KnownNetworks.Add(new HO.IPNetwork(IPAddress.Parse("10.0.0.0"),     8));  // Azure VNET
    options.KnownNetworks.Add(new HO.IPNetwork(IPAddress.Parse("172.16.0.0"),  12));  // Docker / infra
    options.KnownNetworks.Add(new HO.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));  // local dev
    options.KnownNetworks.Add(new HO.IPNetwork(IPAddress.Parse("::ffff:0:0"),  96));  // IPv4-mapped IPv6

    // Only propagate the first (outermost) "real" IP hop to prevent
    // X-Forwarded-For chain spoofing: client→proxy→AppService.
    options.ForwardLimit = 1;
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                       ?? ["http://localhost:5173"];
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              // Explicit method allowlist instead of AllowAnyMethod().
              // Prevents the preflight from advertising methods the frontend
              // never uses and the backend may inadvertently handle.
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              .AllowCredentials();
    });
});

// ── Authentication / JWT ──────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero,
            // Pin the algorithm explicitly to prevent algorithm-confusion attacks.
            // A crafted token with alg=none or alg=RS256 must be rejected even if
            // the signature check incidentally passes.
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        };

        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var jti = ctx.Principal?.FindFirst(
                    System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti))
                {
                    var blocklist = ctx.HttpContext.RequestServices
                        .GetRequiredService<PawTrack.Application.Common.Interfaces.IJtiBlocklist>();

                    if (await blocklist.IsBlockedAsync(jti, ctx.HttpContext.RequestAborted))
                        ctx.Fail("Token has been revoked.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Internal-only health checks — requires Admin JWT or internal network request.
    options.AddPolicy("HealthCheckPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole("Admin"));
});

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // All policies use AddPolicy + RateLimitPartition.GetFixedWindowLimiter so that
    // each client IP gets its own independent quota window (per-IP partitioning).
    //
    // With AddFixedWindowLimiter the counter is GLOBAL — all clients share one pool.
    // One attacker making N requests per window blocks all other users from that
    // endpoint for the remainder of the window (DoS by exhaustion).
    //
    // RateLimiterIpKey.Get(ctx) returns ctx.Connection.RemoteIpAddress?.ToString()
    // ?? "anonymous" — ForwardedHeaders middleware has already unwrapped X-Forwarded-For
    // to the real client IP before the rate-limiter middleware runs.

    // ── Auth: login — 5 attempts/min per IP (brute-force protection) ──────────
    options.AddPolicy("login", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 5),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:Login:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Auth: register — 5 new accounts per 10 min per IP (email-bomb protection) ──
    options.AddPolicy("register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:Register:PermitLimit", 5),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:Register:WindowSeconds", 600)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Auth: refresh — 20 rotations/min per IP (protects token-rotation endpoint) ──
    options.AddPolicy("refresh", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:Refresh:PermitLimit", 20),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:Refresh:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Auth: forgot-password — anti-abuse mail flood protection ─────────────
    options.AddPolicy("forgot-password", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:ForgotPassword:PermitLimit", 5),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:ForgotPassword:WindowSeconds", 600)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Auth: reset-password — token brute-force protection ─────────────────
    options.AddPolicy("reset-password", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:ResetPassword:PermitLimit", 10),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:ResetPassword:WindowSeconds", 600)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Auth: verify-email — 10 attempts/hour per IP (enumeration/replay protection) ──
    options.AddPolicy("verify-email", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:VerifyEmail:PermitLimit", 10),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:VerifyEmail:WindowSeconds", 3600)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Sightings public submission — 5 req/min per IP ───────────────────────
    options.AddPolicy("sightings", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:Sightings:PermitLimit", 5),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:Sightings:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── WhatsApp avatar — public endpoint, heavier image generation: 10 req/min per IP ──
    options.AddPolicy("whatsapp-avatar", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:WhatsAppAvatar:PermitLimit", 10),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:WhatsAppAvatar:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Public read-only API — map, stats, pet profiles, leaderboard ─────────
    // 30 req/min is generous for a human user; automated scrapers will be capped.
    options.AddPolicy("public-api", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:PublicApi:PermitLimit", 30),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:PublicApi:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Handover code verify — brute-force protection ─────────────────────────
    // 4-digit PIN (10 000 combos): capped at 5 attempts/min per IP.
    options.AddPolicy("handover-verify", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:HandoverVerify:PermitLimit", 5),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:HandoverVerify:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Authenticated location update — caps GPS spam from mobile clients ─────
    options.AddPolicy("location-update", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:LocationUpdate:PermitLimit", 60),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:LocationUpdate:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Chat messages — authenticated, 30 msg/min per IP ─────────────────────
    options.AddPolicy("chat-message", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:ChatMessage:PermitLimit", 30),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:ChatMessage:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Broadcast — 3 external notifications per 10-min window per IP ────────
    options.AddPolicy("broadcast", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:Broadcast:PermitLimit", 3),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:Broadcast:WindowSeconds", 600)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Clinic scan — 30 scans/min per IP ────────────────────────────────────
    options.AddPolicy("clinic-scan", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:ClinicScan:PermitLimit", 30),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:ClinicScan:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Notifications write — authenticated write endpoints ───────────────────
    options.AddPolicy("notifications-write", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:NotificationsWrite:PermitLimit", 20),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:NotificationsWrite:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    // ── Contact lookup — returns owner ContactPhone; limited to 10 req/min per IP ──
    options.AddPolicy("contact-lookup", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: RateLimiterIpKey.Get(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = builder.Configuration.GetValue("RateLimiting:ContactLookup:PermitLimit", 10),
                Window               = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimiting:ContactLookup:WindowSeconds", 60)),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));

    options.RejectionStatusCode = 429;
});

// ── Health Checks ─────────────────────────────────────────────────────────────
var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? string.Empty;
var blobConnectionString = builder.Configuration["Azure:Storage:ConnectionString"]
    ?? string.Empty;

builder.Services.AddHealthChecks()
    .AddSqlServer(
        sqlConnectionString,
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "live"])
    .AddAzureBlobStorage(
        _ => new Azure.Storage.Blobs.BlobServiceClient(blobConnectionString),
        name: "blob-storage",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"]);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddResponseCaching();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(options =>
{
    // SearchCoordinationHub only exchanges GUIDs, zone states, and GPS coords.
    // 4 KB ceiling prevents memory exhaustion from oversized client payloads.
    options.MaximumReceiveMessageSize = 4 * 1024; // 4 KB
});

// ── Application Pipeline ──────────────────────────────────────────────────────
var app = builder.Build();

// Guard runs here so WebApplicationFactory's ConfigureWebHost overrides
// (UseEnvironment + ConfigureAppConfiguration) are visible to the check.
StartupGuards.EnsureJwtKeyStrength(app.Configuration, app.Environment);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ── Trusted-proxy unwrapping — MUST come before any middleware that reads IP ──
app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Frontend");
app.UseRateLimiter();
// Enable request body buffering so the WhatsApp signature filter can read
// the body for HMAC validation and the action binder can read it again.
app.Use(async (ctx, next) =>
{
    ctx.Request.EnableBuffering();
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();
app.MapControllers();
app.MapHub<SearchCoordinationHub>("/hubs/search-coordination");

// ── Startup seeders ───────────────────────────────────────────────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
await RiskCalendarEventSeeder.SeedAsync(app.Services, startupLogger);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = WriteHealthCheckResponse,
}).RequireAuthorization("HealthCheckPolicy");

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = WriteHealthCheckResponse,
}).RequireAuthorization("HealthCheckPolicy");

// Minimal live-probe — unauthenticated, reveals nothing sensitive.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // skip all checks — just proves the process is alive
    ResponseWriter = async (ctx, _) =>
    {
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{\"status\":\"Healthy\"}");
    },
});

app.Run();

// ── Health check JSON writer ──────────────────────────────────────────────────
static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var result = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        duration = report.TotalDuration.TotalMilliseconds,
        checks = report.Entries.Select(e => new
        {
            name = e.Key,
            status = e.Value.Status.ToString(),
            duration = e.Value.Duration.TotalMilliseconds,
            description = e.Value.Description,
        }),
    });

    return context.Response.WriteAsync(result);
}

// Expose Program for WebApplicationFactory in integration tests
public partial class Program { }

