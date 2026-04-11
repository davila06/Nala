using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Chat;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ThreadId).IsRequired();
        builder.Property(m => m.SenderUserId).IsRequired();

        builder.Property(m => m.Body)
               .HasMaxLength(800)
               .IsRequired();

        builder.Property(m => m.SentAt).IsRequired();
        builder.Property(m => m.IsReadByRecipient).IsRequired();

        builder.HasIndex(m => m.ThreadId);
        builder.HasIndex(m => new { m.ThreadId, m.SentAt });
    }
}
