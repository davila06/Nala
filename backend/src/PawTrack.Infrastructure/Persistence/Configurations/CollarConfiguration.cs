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
