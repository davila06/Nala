using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class BreedReferenceConfiguration : IEntityTypeConfiguration<BreedReference>
{
    public void Configure(EntityTypeBuilder<BreedReference> b)
    {
        b.ToTable("BreedReferences");
        b.HasKey(r => r.Id);
        b.Property(r => r.BreedKey).HasMaxLength(100).IsRequired();
        b.Property(r => r.DisplayName).HasMaxLength(120).IsRequired();
        b.Property(r => r.Species).HasMaxLength(20).IsRequired();
        b.Property(r => r.WeightLabel).HasMaxLength(80);
        b.Property(r => r.EnergyLevel).HasMaxLength(10);
        b.Property(r => r.WeightMinKg).HasColumnType("decimal(6,2)");
        b.Property(r => r.WeightMaxKg).HasColumnType("decimal(6,2)");
        // Lookup index — the application always queries by BreedKey
        b.HasIndex(r => new { r.BreedKey, r.Species }).IsUnique();
        b.HasIndex(r => new { r.Species, r.IsSpeciesFallback });
    }
}
