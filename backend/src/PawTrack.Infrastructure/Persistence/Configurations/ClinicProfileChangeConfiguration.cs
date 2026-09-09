using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicProfileChangeConfiguration : IEntityTypeConfiguration<ClinicProfileChange>
{
    public void Configure(EntityTypeBuilder<ClinicProfileChange> builder)
    {
        builder.ToTable("ClinicProfileChanges");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProposedProfileJson).IsRequired().HasMaxLength(5000);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.ReviewReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.Status, x.CreatedAt });
    }
}