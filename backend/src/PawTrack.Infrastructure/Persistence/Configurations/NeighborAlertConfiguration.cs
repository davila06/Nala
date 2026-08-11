using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Locations;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class NeighborAlertConfiguration : IEntityTypeConfiguration<NeighborAlert>
{
    public void Configure(EntityTypeBuilder<NeighborAlert> builder)
    {
        builder.ToTable("NeighborAlerts");
        builder.HasKey(x => x.UserId); // one row per user

        builder.Property(x => x.Phone).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Lat).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.Lng).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.RadiusMeters).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.EnrolledAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        // Spatial bounding-box queries filter on lat/lng
        builder.HasIndex(x => new { x.IsActive, x.Lat, x.Lng });
    }
}
