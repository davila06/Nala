using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
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
using PawTrack.Infrastructure.Family;
using PawTrack.Infrastructure.Medical;
using PawTrack.Infrastructure.Sightings;
using PawTrack.Infrastructure.Subscriptions;
using PawTrack.Infrastructure.Persistence;
using PawTrack.Infrastructure.Pets;
using PawTrack.Infrastructure.Safety;
using PawTrack.Infrastructure.Sightings;
using PawTrack.Infrastructure.Storage;
using PawTrack.Application.Common.Settings;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Infrastructure.Bounties;
using PawTrack.Infrastructure.Bundles;
using PawTrack.Infrastructure.Certificates;
using PawTrack.Infrastructure.Collars;
using PawTrack.Infrastructure.Municipalities;
using PawTrack.Infrastructure.Subscriptions;

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
        services.Configure<PersonalDataRetentionSettings>(configuration.GetSection("PersonalDataRetention"));
        services.Configure<AvatarTokenSettings>(configuration.GetSection("AvatarToken"));
        services.Configure<PetScanExportSettings>(configuration.GetSection("PetScanExport"));
        services.Configure<PawTrack.Application.Common.Settings.BotSettings>(configuration.GetSection("Bot"));

        // EF Core
        services.AddDbContext<PawTrackDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                    sqlOptions.UseNetTopologySuite(); // enables geography/geometry spatial types
                });
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PawTrackDbContext>());

        // Distributed job lock — prevents duplicate background job execution on scale-out.
        services.AddSingleton<IDistributedJobLock, SqlServerDistributedJobLock>();

        // Outbox processor — delivers domain events at-least-once after DB commit.
        services.AddHostedService<PawTrack.Infrastructure.Outbox.OutboxProcessorHostedService>();

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
        services.AddScoped<INeighborAlertRepository, NeighborAlertRepository>();
        services.AddScoped<IFoundPetRepository, FoundPetRepository>();
        services.AddScoped<IFosterVolunteerRepository, FosterVolunteerRepository>();
        services.AddScoped<ICustodyRecordRepository, CustodyRecordRepository>();
        services.AddScoped<IStoreRepository, PawTrack.Infrastructure.Stores.StoreRepository>();
        services.AddScoped<IStoreOrderRepository, PawTrack.Infrastructure.Stores.StoreOrderRepository>();

        // Adoptions
        services.AddScoped<IAdoptionRepository, PawTrack.Infrastructure.Adoptions.AdoptionRepository>();

        // Audit log
        services.AddScoped<IAuditLogRepository, PawTrack.Infrastructure.Audit.AuditLogRepository>();

        // Clinics
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IClinicScanRepository, ClinicScanRepository>();
        services.AddScoped<IClinicProfileViewRepository, ClinicProfileViewRepository>();
        services.AddHostedService<ClinicProfileViewPurgeHostedService>();
        services.AddScoped<IClinicApiKeyRepository, ClinicApiKeyRepository>();

        // Push subscriptions
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();

        // Safety (chat + handover + fraud)
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IBillboardRepository, PawTrack.Infrastructure.Advertising.BillboardRepository>();
        services.AddScoped<IHandoverCodeRepository, HandoverCodeRepository>();
        services.AddScoped<IFraudReportRepository, FraudReportRepository>();

        // Auth services
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        // SQL-backed: survives restarts and works across App Service scale-out instances
        services.AddScoped<IJtiBlocklist, DbJtiBlocklist>();
        services.AddHostedService<RevokedTokenCleanupJob>();

        // Storage
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IWhatsAppAvatarService, WhatsAppAvatarComposer>();
        services.AddSingleton<IPublicAppUrlProvider, PublicAppUrlProvider>();

        // Notifications
        services.AddMemoryCache();
        // Prefer Redis (Azure Cache for Redis) in production; fall back to in-memory distributed cache.
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConnectionString);
        }
        else
        {
            services.AddDistributedMemoryCache(); // single-instance dev fallback
        }
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddHttpClient("PushProvider")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();
        services.AddHttpClient("Tractive")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(12))
            .AddStandardResilienceHandler();
        services.AddSingleton<IPushNotificationService, PushNotificationService>();
        services.AddSingleton<INotificationRateLimitService, DistributedNotificationRateLimitService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<StaleReportCheckerJob>();
        services.AddHostedService<StaleReportCheckerHostedService>();
        services.AddHostedService<RiskAlertHostedService>();

        // QR retention job (runs at 02:00 CR time)
        services.AddScoped<QrScanRetentionJob>();
        services.AddHostedService<QrScanRetentionHostedService>();

        // Personal data retention job (sightings, closed chat threads, read notifications;
        // runs at 03:00 CR time) — Ley 8968 proportional conservation principle.
        services.AddScoped<PawTrack.Infrastructure.Compliance.PersonalDataRetentionJob>();
        services.AddHostedService<PawTrack.Infrastructure.Compliance.PersonalDataRetentionHostedService>();

        // Broadcast — channel broadcasters registered as IChannelBroadcaster.
        // The orchestrator resolves IEnumerable<IChannelBroadcaster> to fan out.
        services.AddScoped<IChannelBroadcaster, EmailChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, WhatsAppChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, TelegramChannelBroadcaster>();
        services.AddScoped<IChannelBroadcaster, FacebookChannelBroadcaster>();
        services.AddHttpClient("Telegram")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();
        services.AddHttpClient("Facebook")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();
        services.AddScoped<IMultichannelBroadcastService, MultichannelBroadcastService>();
        services.AddSingleton<ITrackingLinkService, TrackingLinkService>();

        // Sightings
        services.AddSingleton<IPiiScrubber, PiiScrubber>();
        services.AddScoped<IVisualMatchRepository, VisualMatchRepository>();
        services.AddScoped<IAiSearchUsageRepository, AiSearchUsageRepository>();

        // AI — Azure Computer Vision 4.0 embedding service.
        // HttpClient timeout is intentionally short; VectorizeUrlAsync is best-effort.
        services.AddHttpClient("AzureVision")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(12))
            .AddStandardResilienceHandler();
        services.AddSingleton<IImageEmbeddingService, AzureVisionEmbeddingService>();
        services.AddHostedService<EmbeddingRefreshHostedService>();

        // Animal photo validation — reuses AzureVision HttpClient; fail-open by design.
        services.Configure<AnimalPhotoValidationSettings>(
            configuration.GetSection("AnimalPhotoValidation"));
        services.AddScoped<IAnimalPhotoValidator, AzureVisionAnimalPhotoValidator>();

        // WhatsApp Bot — Meta Cloud API sender + Azure Maps geocoder
        services.AddHttpClient("MetaWhatsApp")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15))
            .AddStandardResilienceHandler();
        services.AddHttpClient("AzureMaps")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))
            .AddStandardResilienceHandler();
        services.AddScoped<IBotSessionRepository, BotSessionRepository>();
        services.AddScoped<IWhatsAppIdempotencyRepository, WhatsAppIdempotencyRepository>();
        services.AddScoped<IWhatsAppSender, MetaWhatsAppSender>();
        services.AddScoped<IGeocodingService, AzureMapsGeocodingService>();
        services.AddScoped<IReverseGeocodingService, AzureMapsGeocodingService>();
        services.AddSingleton<IWhatsAppSettings, WhatsAppSettings>();

        // IP geo-lookup (Azure Maps Geolocation API — reuses "AzureMaps" HttpClient)
        services.AddScoped<IIpGeoLookupService, AzureMapsIpGeoLookupService>();

        // Ephemeral avatar token service (HMAC-SHA256, stateless)
        services.AddSingleton<IAvatarTokenService, HmacAvatarTokenService>();

        // Subscriptions + payments
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddSingleton<IPaymentService, SinpePaymentService>();
        services.AddHostedService<SubscriptionExpirationJob>();
        services.AddHostedService<SubscriptionRenewalNotificationJob>();

        // Promotions
        services.AddScoped<PawTrack.Application.Promotions.Interfaces.IPromotionCodeRepository,
            PawTrack.Infrastructure.Promotions.PromotionCodeRepository>();

        // Bounties
        services.AddScoped<IBountyRepository, BountyRepository>();
        services.AddScoped<IBundleOrderRepository, BundleOrderRepository>();

        // Collars / GPS
        services.AddScoped<ICollarRepository, CollarRepository>();
        services.AddScoped<ICollarTagRepository, CollarTagRepository>();
        services.AddScoped<ICollarDeviceCredentialRepository, CollarDeviceCredentialRepository>();
        services.AddScoped<ICollarAuditRepository, CollarAuditRepository>();
        services.AddScoped<ICollarHandoverCodeRepository, CollarHandoverCodeRepository>();
        services.AddScoped<ICollarSafeZoneRepository, CollarSafeZoneRepository>();
        services.AddScoped<PawTrack.Application.Collars.Services.CollarConnectivityAlertService>();
        services.AddScoped<PawTrack.Application.Collars.Services.CollarSafeZoneEvaluationService>();

        // PDF Certificates
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<ICertificateService, QuestPdfCertificateService>();
        services.AddScoped<IMedicalPdfExporter, QuestPdfMedicalExporter>();
        services.AddScoped<IAnnualReportPdfGenerator, QuestPdfAnnualReportGenerator>();
        services.AddScoped<IPetIdCardService, QuestPdfIdCardService>();

        // Family accounts
        services.AddScoped<IFamilyRepository, FamilyRepository>();

        // Medical records + vet reminders
        services.AddScoped<IMedicalRepository, MedicalRepository>();
        services.AddScoped<IBreedReferenceRepository, BreedReferenceRepository>();
        services.AddHostedService<BreedReferenceSeedHostedService>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IClinicMedicalAccessLogRepository, ClinicMedicalAccessLogRepository>();
        services.AddScoped<IClinicMedicalAccessGrantRepository, ClinicMedicalAccessGrantRepository>();
        services.AddScoped<PawTrack.Infrastructure.Medical.VetReminderNotificationJob>();
        services.AddHostedService<PawTrack.Infrastructure.Medical.VetReminderHostedService>();
        services.AddScoped<PawTrack.Infrastructure.Medical.HealthAlertJob>();
        services.AddHostedService<PawTrack.Infrastructure.Medical.HealthAlertHostedService>();

        // Municipalities
        services.AddScoped<ICapturedAnimalRepository, CapturedAnimalRepository>();
        services.AddScoped<IMunicipalProfileRepository, MunicipalProfileRepository>();
        services.AddScoped<IMunicipalSubscriptionService, MunicipalSubscriptionService>();

        // Tractive GPS polling (runs every 5 min)
        services.AddSingleton<ITractiveService, TractiveService>();
        services.AddHostedService<TractivePollingJob>();
        services.AddHostedService<CollarLocationPurgeJob>();
        services.AddHostedService<CollarConnectivityAlertJob>();

        return services;
    }
}
