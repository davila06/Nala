using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.ProductAnalytics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ProductEventConfiguration : IEntityTypeConfiguration<ProductEvent>
{
    public void Configure(EntityTypeBuilder<ProductEvent> builder)
    {
        builder.ToTable("ProductEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventId).IsRequired();
        builder.Property(x => x.EventName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SchemaVersion).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AnonymousId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Canton).HasMaxLength(100);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.OccurredAt).IsRequired();
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.HasIndex(x => x.EventId).IsUnique();
        builder.HasIndex(x => new { x.EventName, x.OccurredAt });
        builder.HasIndex(x => new { x.Canton, x.OccurredAt });
    }
}