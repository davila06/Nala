using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionAddonConfiguration : IEntityTypeConfiguration<SubscriptionAddon>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddon> builder)
    {
        builder.ToTable("SubscriptionAddons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntitlementKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Units).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.PriceCrc).HasColumnType("decimal(12,2)");
        builder.HasIndex(x => new { x.SubscriptionId, x.EntitlementKey, x.StartsAt, x.ExpiresAt });
        builder.HasIndex(x => new { x.SubscriptionId, x.IsActive });
    }
}
