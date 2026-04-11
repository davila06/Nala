using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Bot;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BotSessionConfiguration : IEntityTypeConfiguration<BotSession>
{
    public void Configure(EntityTypeBuilder<BotSession> builder)
    {
        builder.ToTable("BotSessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.PhoneNumberHash)
               .HasMaxLength(64)    // SHA-256 hex = 64 chars
               .IsRequired();

        builder.Property(s => s.Step)
               .HasConversion<string>()
               .HasMaxLength(30)
               .IsRequired();

        builder.Property(s => s.PetName).HasMaxLength(100);
        builder.Property(s => s.LastSeenRaw).HasMaxLength(200);
        builder.Property(s => s.LocationRaw).HasMaxLength(300);

        builder.Property(s => s.ProcessedMessageIds)
               .HasMaxLength(2000)     // ~50 wamids × 40 chars each
               .IsRequired()
               .HasDefaultValue("[]");

        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();

        // Look up active sessions by phone hash (most-frequent read pattern)
        builder.HasIndex(s => new { s.PhoneNumberHash, s.Step });
        builder.HasIndex(s => s.ExpiresAt);
    }
}
