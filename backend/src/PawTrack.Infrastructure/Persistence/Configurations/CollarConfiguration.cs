using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Collars;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CollarConfiguration : IEntityTypeConfiguration<Collar>
{
    public void Configure(EntityTypeBuilder<Collar> builder)
    {
        builder.ToTable("Collars");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Provider).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ExternalDeviceId).HasMaxLength(100);
        builder.Property(x => x.ExternalTokenEncrypted).HasMaxLength(1000);
        builder.Property(x => x.BatteryPercent);
        builder.Property(x => x.LastLat);
        builder.Property(x => x.LastLng);
        builder.Property(x => x.LastSeenAt);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RegisteredAt).IsRequired();
        builder.Property(x => x.CollarTagSerial).HasMaxLength(30);
        builder.Property(x => x.OfflineAlertsEnabled).IsRequired();
        builder.Property(x => x.OfflineThresholdMinutes).IsRequired();
        builder.Property(x => x.IsOffline).IsRequired();
        builder.Property(x => x.BatteryAlertsEnabled).IsRequired();
        builder.Property(x => x.BatteryAlertThresholdPercent).IsRequired();
        builder.Property(x => x.IsLost).IsRequired();
        builder.Property(x => x.LostModeActivatedAt);
        builder.Property(x => x.LostPetEventId);

        builder.HasIndex(x => new { x.PetId, x.IsActive });
    }
}

public sealed class CollarLocationConfiguration : IEntityTypeConfiguration<CollarLocation>
{
    public void Configure(EntityTypeBuilder<CollarLocation> builder)
    {
        builder.ToTable("CollarLocations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CollarId).IsRequired();
        builder.Property(x => x.Lat).IsRequired();
        builder.Property(x => x.Lng).IsRequired();
        builder.Property(x => x.Accuracy);
        builder.Property(x => x.RecordedAt).IsRequired();

        // Supports time-range queries and auto-purge of old points
        builder.HasIndex(x => new { x.CollarId, x.RecordedAt });
    }
}

public sealed class CollarTagConfiguration : IEntityTypeConfiguration<CollarTag>
{
    public void Configure(EntityTypeBuilder<CollarTag> builder)
    {
        builder.ToTable("CollarTags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Serial).IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.Serial).IsUnique();

        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FirmwareVersion).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ManufacturedAt).IsRequired();
        builder.Property(x => x.SoldAt);
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.LastPingAt);
        builder.Property(x => x.CollarId);
    }
}

public sealed class CollarAuditEntryConfiguration : IEntityTypeConfiguration<CollarAuditEntry>
{
    public void Configure(EntityTypeBuilder<CollarAuditEntry> builder)
    {
        builder.ToTable("CollarAuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CollarId);
        builder.Property(x => x.Serial).HasMaxLength(30);
        builder.Property(x => x.UserId);
        builder.Property(x => x.Event).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Details).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.CollarId, x.CreatedAt });
        builder.HasIndex(x => new { x.Serial, x.CreatedAt });
    }
}

public sealed class CollarHandoverCodeConfiguration : IEntityTypeConfiguration<CollarHandoverCode>
{
    public void Configure(EntityTypeBuilder<CollarHandoverCode> builder)
    {
        builder.ToTable("CollarHandoverCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CollarId).IsRequired();
        builder.Property(x => x.GeneratedByOwnerId).IsRequired();
        builder.Property(x => x.PinHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AttemptCount).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.RedeemedAt);
        builder.Property(x => x.RedeemedByUserId);
        builder.Property(x => x.CancelledAt);

        builder.HasIndex(x => new { x.CollarId, x.RedeemedAt, x.CancelledAt });
    }
}

public sealed class CollarSafeZoneConfiguration : IEntityTypeConfiguration<CollarSafeZone>
{
    public void Configure(EntityTypeBuilder<CollarSafeZone> builder)
    {
        builder.ToTable("CollarSafeZones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CollarId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PolygonJson).IsRequired();
        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastKnownInside);

        builder.HasIndex(x => new { x.CollarId, x.Enabled });
    }
}

public sealed class CollarDeviceCredentialConfiguration : IEntityTypeConfiguration<CollarDeviceCredential>
{
    public void Configure(EntityTypeBuilder<CollarDeviceCredential> builder)
    {
        builder.ToTable("CollarDeviceCredentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CollarId).IsRequired();
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(64);
        // Index for O(1) lookup on each ingest request
        builder.HasIndex(x => x.KeyHash);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.LastUsedAt);
    }
}
