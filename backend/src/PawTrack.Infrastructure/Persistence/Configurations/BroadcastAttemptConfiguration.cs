using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Broadcast;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BroadcastAttemptConfiguration : IEntityTypeConfiguration<BroadcastAttempt>
{
    public void Configure(EntityTypeBuilder<BroadcastAttempt> builder)
    {
        builder.ToTable("BroadcastAttempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.LostPetEventId).IsRequired();

        builder.Property(a => a.Channel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.ExternalId).HasMaxLength(200);
        builder.Property(a => a.TrackingUrl).HasMaxLength(500);
        builder.Property(a => a.ErrorMessage).HasMaxLength(500);

        builder.Property(a => a.TrackingClicks).IsRequired().HasDefaultValue(0);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.SentAt);

        // Frequently queried by event ID to build the broadcast status panel.
        builder.HasIndex(a => a.LostPetEventId);
        // Supports the per-channel retry query (find failed attempts for a given event + channel).
        builder.HasIndex(a => new { a.LostPetEventId, a.Channel });
    }
}
