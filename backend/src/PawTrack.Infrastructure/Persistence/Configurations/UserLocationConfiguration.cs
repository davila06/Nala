using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Locations;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
{
    public void Configure(EntityTypeBuilder<UserLocation> builder)
    {
        builder.ToTable("UserLocations");

        // UserId is both the PK and the FK — one row per user.
        builder.HasKey(u => u.UserId);
        builder.Property(u => u.UserId).ValueGeneratedNever();

        builder.Property(u => u.Lat).IsRequired();
        builder.Property(u => u.Lng).IsRequired();
        builder.Property(u => u.ReceiveNearbyAlerts).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // Quiet-hours window — stored as SQL time(0) in Costa Rica local (UTC-6).
        // Both columns are nullable; null signals "no quiet window configured".
        builder.Property(u => u.QuietHoursStart)
               .HasColumnType("time(0)")
               .IsRequired(false);

        builder.Property(u => u.QuietHoursEnd)
               .HasColumnType("time(0)")
               .IsRequired(false);

        // Index to accelerate bounding-box queries in GetNearbyAlertSubscribersAsync.
        builder.HasIndex(u => new { u.ReceiveNearbyAlerts, u.Lat, u.Lng })
               .HasDatabaseName("IX_UserLocations_Alerts_Lat_Lng");
    }
}
