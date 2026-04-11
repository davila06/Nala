using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Chat;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ChatThreadConfiguration : IEntityTypeConfiguration<ChatThread>
{
    public void Configure(EntityTypeBuilder<ChatThread> builder)
    {
        builder.ToTable("ChatThreads");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.LostPetEventId).IsRequired();
        builder.Property(t => t.InitiatorUserId).IsRequired();
        builder.Property(t => t.OwnerUserId).IsRequired();

        builder.Property(t => t.Status)
               .HasConversion<string>()
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.LastMessageAt).IsRequired();
        builder.Property(t => t.FlagReason).HasMaxLength(500);

        // Navigation: messages owned by thread.
        builder.HasMany(t => t.Messages)
               .WithOne()
               .HasForeignKey(m => m.ThreadId)
               .OnDelete(DeleteBehavior.Cascade);

        // One active thread per finder per event (soft constraint — enforced by handler).
        builder.HasIndex(t => new { t.LostPetEventId, t.InitiatorUserId });
        builder.HasIndex(t => t.OwnerUserId);
    }
}
