using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Bot;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Broadcast;
using PawTrack.Domain.Bundles;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Chat;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Family;
using PawTrack.Domain.Fosters;
using PawTrack.Domain.Incentives;
using PawTrack.Domain.Locations;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Notifications;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Promotions;
using PawTrack.Domain.Safety;
using PawTrack.Domain.Sightings;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence;

public sealed class PawTrackDbContext(DbContextOptions<PawTrackDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AllyProfile> AllyProfiles => Set<AllyProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<QrScanEvent> QrScanEvents => Set<QrScanEvent>();
    public DbSet<LostPetEvent> LostPetEvents => Set<LostPetEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserNotificationPreferences> UserNotificationPreferences => Set<UserNotificationPreferences>();
    public DbSet<RiskCalendarEvent> RiskCalendarEvents => Set<RiskCalendarEvent>();
    public DbSet<Sighting> Sightings => Set<Sighting>();
    public DbSet<FoundPetReport> FoundPetReports => Set<FoundPetReport>();
    public DbSet<FosterVolunteer> FosterVolunteers => Set<FosterVolunteer>();
    public DbSet<CustodyRecord> CustodyRecords => Set<CustodyRecord>();
    public DbSet<UserLocation> UserLocations => Set<UserLocation>();
    public DbSet<BroadcastAttempt> BroadcastAttempts => Set<BroadcastAttempt>();
    public DbSet<ContributorScore> ContributorScores => Set<ContributorScore>();
    public DbSet<GeofencedAlertLog> GeofencedAlertLogs => Set<GeofencedAlertLog>();
    public DbSet<PetPhotoEmbedding> PetPhotoEmbeddings => Set<PetPhotoEmbedding>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<HandoverCode> HandoverCodes => Set<HandoverCode>();
    public DbSet<FraudReport> FraudReports => Set<FraudReport>();
    public DbSet<BotSession> BotSessions => Set<BotSession>();
    public DbSet<SearchZone> SearchZones => Set<SearchZone>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<ClinicScan> ClinicScans => Set<ClinicScan>();
    public DbSet<ClinicProfileView> ClinicProfileViews => Set<ClinicProfileView>();
    public DbSet<ClinicApiKey> ClinicApiKeys => Set<ClinicApiKey>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Bounty> Bounties => Set<Bounty>();
    public DbSet<BundleOrder> BundleOrders => Set<BundleOrder>();
    public DbSet<Collar> Collars => Set<Collar>();
    public DbSet<CollarLocation> CollarLocations => Set<CollarLocation>();
    public DbSet<VetCertificate> VetCertificates => Set<VetCertificate>();
    public DbSet<CapturedAnimal> CapturedAnimals => Set<CapturedAnimal>();
    public DbSet<MunicipalityProfile> MunicipalityProfiles => Set<MunicipalityProfile>();
    public DbSet<AiSearchUsage> AiSearchUsages => Set<AiSearchUsage>();
    public DbSet<FamilyAccount> FamilyAccounts => Set<FamilyAccount>();
    public DbSet<FamilyMembership> FamilyMemberships => Set<FamilyMembership>();
    public DbSet<FamilyInvitation> FamilyInvitations => Set<FamilyInvitation>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<VetReminder> VetReminders => Set<VetReminder>();
    public DbSet<ClinicMedicalAccessGrant> ClinicMedicalAccessGrants => Set<ClinicMedicalAccessGrant>();
    public DbSet<ClinicMedicalAccessLog> ClinicMedicalAccessLogs => Set<ClinicMedicalAccessLog>();
    public DbSet<PromotionCode> PromotionCodes => Set<PromotionCode>();
    public DbSet<PromotionCodeRedemption> PromotionCodeRedemptions => Set<PromotionCodeRedemption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PawTrackDbContext).Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // NoTracking by default — opt-in with .AsTracking() for write operations
        if (!optionsBuilder.IsConfigured) return;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}
