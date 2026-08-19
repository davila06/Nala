using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Advertising;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class BillboardConfiguration : IEntityTypeConfiguration<Billboard>
{
    public void Configure(EntityTypeBuilder<Billboard> builder)
    {
        builder.ToTable("Billboards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Body).HasMaxLength(300);
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.CtaLabel).HasMaxLength(60);
        builder.Property(x => x.CtaUrl).HasMaxLength(500);
        builder.Property(x => x.Placement).IsRequired().HasConversion<int>();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.Placement, x.Status, x.StartsAt, x.EndsAt });
    }
}
