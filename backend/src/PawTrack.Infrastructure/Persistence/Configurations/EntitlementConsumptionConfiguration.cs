using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class EntitlementConsumptionConfiguration : IEntityTypeConfiguration<EntitlementConsumption>
{
    public void Configure(EntityTypeBuilder<EntitlementConsumption> builder)
    {
        builder.ToTable("EntitlementConsumptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntitlementKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Units).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContextType).HasMaxLength(80);
        builder.HasIndex(x => new { x.SubjectId, x.EntitlementKey, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.SubjectId, x.EntitlementKey, x.CycleStart, x.CycleEnd });
    }
}
