using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.ToTable("PlanEntitlements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntitlementKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ValueType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.NumericValue).HasPrecision(18, 4);
        builder.Property(x => x.TextValue).HasMaxLength(500);
        builder.Property(x => x.Unit).HasMaxLength(40);
        builder.Property(x => x.ResetPeriod).HasMaxLength(40);
        builder.HasIndex(x => new { x.PlanId, x.EntitlementKey }).IsUnique();
        builder.HasIndex(x => new { x.PlanId, x.IsActive });
    }
}
