using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.UserId).IsRequired();
        builder.HasIndex(c => c.UserId).IsUnique(); // one clinic per user account

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(c => c.LicenseNumber).IsUnique();

        builder.Property(c => c.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.Lat)
            .IsRequired()
            .HasColumnType("decimal(9,6)");

        builder.Property(c => c.Lng)
            .IsRequired()
            .HasColumnType("decimal(9,6)");

        builder.Property(c => c.ContactEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Status);

        builder.Property(c => c.RegisteredAt).IsRequired();
    }
}
