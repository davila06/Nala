using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.CastrationCampaigns;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CastrationCampaignConfiguration : IEntityTypeConfiguration<CastrationCampaign>
{
    public void Configure(EntityTypeBuilder<CastrationCampaign> builder)
    {
        builder.ToTable("CastrationCampaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.VenueLabel).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Canton).IsRequired().HasMaxLength(80);
        builder.Property(x => x.ConsentVersion).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.BasePriceCrc).HasColumnType("decimal(12,2)");
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasIndex(x => new { x.Status, x.StartsAt, x.Canton });
        builder.HasIndex(x => new { x.OrganizerUserId, x.Status });
        builder.HasIndex(x => new { x.ExecutingClinicId, x.Status });
    }
}
