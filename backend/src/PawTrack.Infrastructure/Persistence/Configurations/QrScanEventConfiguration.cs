using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Pets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class QrScanEventConfiguration : IEntityTypeConfiguration<QrScanEvent>
{
    public void Configure(EntityTypeBuilder<QrScanEvent> builder)
    {
        builder.ToTable("QrScanEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.PetId).IsRequired();
        builder.Property(e => e.ScannedByUserId).HasMaxLength(64);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.CountryCode).HasMaxLength(8);
        builder.Property(e => e.CityName).HasMaxLength(120);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.ScannedAt).IsRequired();

        builder.HasIndex(e => new { e.PetId, e.ScannedAt })
            .HasDatabaseName("IX_QrScanEvents_PetId_ScannedAt");
    }
}
