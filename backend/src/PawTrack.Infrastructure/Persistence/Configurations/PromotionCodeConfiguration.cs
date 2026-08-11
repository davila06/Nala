using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Promotions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class PromotionCodeConfiguration : IEntityTypeConfiguration<PromotionCode>
{
    public void Configure(EntityTypeBuilder<PromotionCode> builder)
    {
        builder.ToTable("PromotionCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Type).IsRequired().HasConversion<int>();
        builder.Property(x => x.AdminNote).HasMaxLength(500);

        // Optimistic concurrency on counter prevents over-redemption under load
        builder.Property(x => x.RedeemedCount).IsConcurrencyToken();

        // Fast lookup by code (case-insensitive via collation)
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class PromotionCodeRedemptionConfiguration : IEntityTypeConfiguration<PromotionCodeRedemption>
{
    public void Configure(EntityTypeBuilder<PromotionCodeRedemption> builder)
    {
        builder.ToTable("PromotionCodeRedemptions");
        builder.HasKey(x => x.Id);

        // Enforce per-user per-code once at DB level
        builder.HasIndex(x => new { x.UserId, x.PromotionCodeId }).IsUnique();
        builder.HasIndex(x => x.PromotionCodeId);
    }
}
