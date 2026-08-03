using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicProfileViewConfiguration : IEntityTypeConfiguration<ClinicProfileView>
{
    public void Configure(EntityTypeBuilder<ClinicProfileView> builder)
    {
        builder.ToTable("ClinicProfileViews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.ViewedAt).IsRequired();
        builder.Property(x => x.Source).IsRequired().HasMaxLength(50);
        builder.Property(x => x.IpHash).HasMaxLength(64);

        builder.HasIndex(x => new { x.ClinicId, x.ViewedAt });
        builder.HasIndex(x => x.ViewedAt); // for pruning job
    }
}
