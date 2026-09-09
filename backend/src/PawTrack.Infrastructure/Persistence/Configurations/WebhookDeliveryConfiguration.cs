using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Webhooks;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("WebhookDeliveries"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventType).IsRequired().HasMaxLength(120); builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(100); builder.Property(x => x.Signature).IsRequired().HasMaxLength(200);
        builder.Property(x => x.LastError).HasMaxLength(1000); builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }); builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}