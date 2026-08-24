using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Bot;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class WhatsAppProcessedMessageConfiguration : IEntityTypeConfiguration<WhatsAppProcessedMessage>
{
    public void Configure(EntityTypeBuilder<WhatsAppProcessedMessage> b)
    {
        b.ToTable("WhatsAppProcessedMessages");
        b.HasKey(m => m.Id);
        b.Property(m => m.Wamid).HasMaxLength(100).IsRequired();
        // Unique constraint is the idempotency guard — INSERT fails on duplicate wamid.
        b.HasIndex(m => m.Wamid).IsUnique();
    }
}
