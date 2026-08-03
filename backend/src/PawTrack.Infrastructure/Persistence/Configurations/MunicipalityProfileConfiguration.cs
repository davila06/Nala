using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class MunicipalityProfileConfiguration : IEntityTypeConfiguration<MunicipalityProfile>
{
    public void Configure(EntityTypeBuilder<MunicipalityProfile> builder)
    {
        builder.ToTable("MunicipalityProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.Property(x => x.Canton).IsRequired().HasMaxLength(80);
        builder.Property(x => x.OrgName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Tier).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AdditionalCantons).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.SubscribedAt).IsRequired();
        builder.Property(x => x.ExpiresAt);

        builder.HasIndex(x => x.Canton);
    }
}
