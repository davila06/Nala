using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.DistanceMeters);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.Source).IsRequired().HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();

        // Queries by pet + date range
        builder.HasIndex(x => new { x.PetId, x.Date });
        // Dedup index for Tractive sync
        builder.HasIndex(x => new { x.PetId, x.Source, x.Date });
    }
}
