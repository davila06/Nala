using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Allies;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class AllyProfileConfiguration : IEntityTypeConfiguration<AllyProfile>
{
    public void Configure(EntityTypeBuilder<AllyProfile> builder)
    {
        builder.ToTable("AllyProfiles");

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();

        builder.Property(x => x.OrganizationName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.AllyType).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.CoverageLabel).IsRequired().HasMaxLength(120);
        builder.Property(x => x.CoverageLat).IsRequired();
        builder.Property(x => x.CoverageLng).IsRequired();
        builder.Property(x => x.CoverageRadiusMetres).IsRequired();
        builder.Property(x => x.VerificationStatus).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AppliedAt).IsRequired();
        builder.Property(x => x.VerifiedAt);

        builder.HasIndex(x => x.VerificationStatus);
    }
}