using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Sightings;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class AiSearchUsageConfiguration : IEntityTypeConfiguration<AiSearchUsage>
{
    public void Configure(EntityTypeBuilder<AiSearchUsage> builder)
    {
        builder.ToTable("AiSearchUsages");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.UserId).IsRequired();
        builder.Property(u => u.YearMonth).IsRequired();
        builder.Property(u => u.Count).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // One row per user per calendar month
        builder.HasIndex(u => new { u.UserId, u.YearMonth }).IsUnique();
    }
}
