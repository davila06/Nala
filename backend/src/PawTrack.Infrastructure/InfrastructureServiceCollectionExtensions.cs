using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.AI;
using PawTrack.Infrastructure.Allies;
using PawTrack.Infrastructure.Auth;
using PawTrack.Infrastructure.Bot;
using PawTrack.Infrastructure.Broadcast;
using PawTrack.Infrastructure.Broadcast.Channels;
using PawTrack.Infrastructure.Chat;
using PawTrack.Infrastructure.Clinics;
using PawTrack.Infrastructure.Configuration;
using PawTrack.Infrastructure.Fosters;
using PawTrack.Infrastructure.Incentives;
using PawTrack.Infrastructure.Locations;
using PawTrack.Infrastructure.LostPets;
using PawTrack.Infrastructure.Notifications;
using PawTrack.Infrastructure.Notifications.Jobs;
using PawTrack.Infrastructure.Persistence;
using PawTrack.Infrastructure.Pets;
using PawTrack.Infrastructure.Safety;
using PawTrack.Infrastructure.Sightings;
using PawTrack.Infrastructure.Storage;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Options bindings ──────────────────────────────────────────────────
        services.Configure<ResolveCheckSettings>(configuration.GetSection("ResolveCheck"));
        services.Configure<QrScanRetentionSettings>(configuration.GetSection("QrScanRetention"));
        services.Configure<AvatarTokenSettings>(configuration.GetSection("AvatarToken"));
        services.Configure<PetScanExportSettings>(configuration.GetSection("PetScanExport"));

        // EF Core
        services.AddDbContext<PawTrackDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null));
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PawTrackDbContext>());

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAllyProfileRepository, AllyProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IQrScanEventRepository, QrScanEventRepository>();
        services.AddScoped<ILostPetRepository, LostPetRepository>();
        services.AddScoped<IRecoveryStatsReadRepository, RecoveryStatsReadRepository>();
        services.AddScoped<ISearchZoneRepository, SearchZoneRepository>();
        services.AddScoped<ISearchZoneGenerator, SearchZoneGenerator>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserNotificationPreferencesRepository, UserNotificationPreferencesRepository>();
        services.AddScoped<IRiskCalendarEventRepository, RiskCalendarEventRepository>();
        services.AddScoped<ISightingRepository, SightingRepository>();
        services.AddScoped<IUserLocationRepository, UserLocationRepository>();
        services.AddScoped<IBroadcastAttemptRepository, BroadcastAttemptRepository>();
        services.AddScoped<IContributorScoreRepository, ContributorScoreRepository>();
        services.AddScoped<IGeofencedAlertLogRepository, GeofencedAlertLogRepository>();
        services.AddScoped<IFoundPetRepository, FoundPetRepository>();
        services.AddScoped<IFosterVolunteerRepository, FosterVolunteerRepository>();
        services.AddScoped<ICustodyRecordRepository, CustodyRecordRepository>();

        // Clinics
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicScanRepository, ClinicScanRepository>();

        // Push subscriptions
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

        // Safety (chat + handover + fraud)
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IHandoverCodeRepository, HandoverCodeRepository>();
        services.AddScoped<IFraudReportRepository, FraudReportRepository>();

        // Auth services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJtiBlocklist, InMemoryJtiBlocklist>();

        // Storage
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IWhatsAppAvatarService, WhatsAppAvatarComposer>();
        services.AddSingleton<IPublicAppUrlProvider, PublicAppUrlProvider>();

        // Notifications
        services.AddMemoryCache();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddHttpClient("PushProvider")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddSingleton<IPushNotificationService, PushNotificationService>();
        services.AddSingleton<INotificationRateLimitService, MemoryCacheNotificationRateLimitService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<StaleReportCheckerJob>();
        services.AddHostedService<StaleReportCheckerHostedService>();
        services.AddHostedService<RiskAlertHostedService>();

        // QR retention job (runs at 02:00 CR time)
        services.AddScoped<QrScanRetentionJob>();
        services.AddHostedService<QrScanRetentionHostedService>();

        // Broadcast — channel broadcasters registered as IChannelBroadcaster.
        // The orchestrator resolves IEnumerable<IChannelBroadcaster> to fan out.
        services.AddScoped<IChannelBroadcaster, EmailChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, WhatsAppChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, TelegramChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, FacebookChannelBroadcaster>();
        services.AddScoped<IMultichannelBroadcastService, MultichannelBroadcastService>();
        services.AddSingleton<ITrackingLinkService, TrackingLinkService>();

        // Sightings
        services.AddSingleton<IPiiScrubber, PiiScrubber>();
        services.AddScoped<IVisualMatchRepository, VisualMatchRepository>();

        // AI — Azure Computer Vision 4.0 embedding service.
        // HttpClient timeout is intentionally short; VectorizeUrlAsync is best-effort.
        services.AddHttpClient("AzureVision")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(12));
        services.AddSingleton<IImageEmbeddingService, AzureVisionEmbeddingService>();
        services.AddHostedService<EmbeddingRefreshHostedService>();

        // WhatsApp Bot — Meta Cloud API sender + Azure Maps geocoder
        services.AddHttpClient("MetaWhatsApp")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15));
        services.AddHttpClient("AzureMaps")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<IBotSessionRepository, BotSessionRepository>();
        services.AddScoped<IWhatsAppSender, MetaWhatsAppSender>();
        services.AddScoped<IGeocodingService, AzureMapsGeocodingService>();
        services.AddScoped<IReverseGeocodingService, AzureMapsGeocodingService>();
        services.AddSingleton<IWhatsAppSettings, WhatsAppSettings>();

        // IP geo-lookup (Azure Maps Geolocation API — reuses "AzureMaps" HttpClient)
        services.AddScoped<IIpGeoLookupService, AzureMapsIpGeoLookupService>();

        // Ephemeral avatar token service (HMAC-SHA256, stateless)
        services.AddSingleton<IAvatarTokenService, HmacAvatarTokenService>();

        return services;
    }
}
