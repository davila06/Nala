using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Bundles;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BundleOrderConfiguration : IEntityTypeConfiguration<BundleOrder>
{
    public void Configure(EntityTypeBuilder<BundleOrder> builder)
    {
        builder.ToTable("BundleOrders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CollarModel).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.PaymentReference).IsRequired().HasMaxLength(8);
        builder.Property(x => x.AmountCrc).IsRequired().HasColumnType("decimal(10,2)");

        builder.Property(x => x.ShippingFullName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.ShippingAddress).IsRequired().HasMaxLength(300);
        builder.Property(x => x.ShippingCanton).IsRequired().HasMaxLength(80);
        builder.Property(x => x.ShippingPhone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.DeliveryNotes).HasMaxLength(300);

        builder.Property(x => x.TrackingNumber).HasMaxLength(100);
        builder.Property(x => x.AdminNotes).HasMaxLength(500);
        builder.Property(x => x.ActivatedSubscriptionId);
        builder.Property(x => x.PaymentReportedByUser).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PaymentReference).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
