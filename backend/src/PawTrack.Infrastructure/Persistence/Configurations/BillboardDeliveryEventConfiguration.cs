using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Advertising;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BillboardDeliveryEventConfiguration : IEntityTypeConfiguration<BillboardDeliveryEvent>
{
    public void Configure(EntityTypeBuilder<BillboardDeliveryEvent> builder)
    {
        builder.ToTable("BillboardDeliveryEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.EventKeyHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.VisitorHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Canton).HasMaxLength(100);
        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.RecordedAt).IsRequired();
        builder.HasIndex(x => new { x.BillboardId, x.EventType, x.EventKeyHash }).IsUnique();
        builder.HasIndex(x => new { x.BillboardId, x.VisitorHash, x.OccurredOn, x.EventType });
        builder.HasIndex(x => new { x.BillboardId, x.OccurredOn, x.EventType });
    }
}