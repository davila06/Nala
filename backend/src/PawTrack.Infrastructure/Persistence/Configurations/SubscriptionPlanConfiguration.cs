using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Tier).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.MonthlyPriceCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.AnnualPriceCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Tier).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}