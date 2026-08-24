using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Outbox;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("OutboxMessages");
        b.HasKey(m => m.Id);
        b.Property(m => m.MessageType).HasMaxLength(250).IsRequired();
        b.Property(m => m.Payload).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(m => m.Status).HasConversion<string>().HasMaxLength(15);
        b.Property(m => m.Error).HasMaxLength(1000);
        // Index used by the processor to fetch pending messages efficiently.
        b.HasIndex(m => new { m.Status, m.CreatedAt });
    }
}
