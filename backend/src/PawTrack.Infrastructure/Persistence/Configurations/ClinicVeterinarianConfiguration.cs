using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicVeterinarianConfiguration : IEntityTypeConfiguration<ClinicVeterinarian>
{
    public void Configure(EntityTypeBuilder<ClinicVeterinarian> builder)
    {
        builder.ToTable("ClinicVeterinarians");
        builder.HasKey(veterinarian => veterinarian.Id);
        builder.Property(veterinarian => veterinarian.Id).ValueGeneratedNever();
        builder.Property(veterinarian => veterinarian.ClinicId).IsRequired();
        builder.Property(veterinarian => veterinarian.FullName).IsRequired().HasMaxLength(120);
        builder.Property(veterinarian => veterinarian.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.Property(veterinarian => veterinarian.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(veterinarian => veterinarian.DocumentUrl).HasMaxLength(500);
        builder.Property(veterinarian => veterinarian.SignatureImageUrl).HasMaxLength(500);
        builder.Property(veterinarian => veterinarian.SubmittedByUserId).IsRequired();
        builder.Property(veterinarian => veterinarian.ReviewedByAdminUserId);
        builder.Property(veterinarian => veterinarian.ReviewedAt);
        builder.Property(veterinarian => veterinarian.ExpiresAt);
        builder.Property(veterinarian => veterinarian.ReviewNotes).HasMaxLength(500);
        builder.Property(veterinarian => veterinarian.RejectionReason).HasMaxLength(300);
        builder.Property(veterinarian => veterinarian.SuspensionReason).HasMaxLength(300);
        builder.Property(veterinarian => veterinarian.CreatedAt).IsRequired();
        builder.Property(veterinarian => veterinarian.RevokedAt);
        builder.Property(veterinarian => veterinarian.RevokedByUserId);
        builder.Property(veterinarian => veterinarian.RevocationReason).HasMaxLength(300);

        builder.Ignore(veterinarian => veterinarian.CanIssueCertificates);
        builder.Ignore(veterinarian => veterinarian.IsActive);

        builder.HasIndex(veterinarian => new { veterinarian.ClinicId, veterinarian.LicenseNumber }).IsUnique();
        builder.HasIndex(veterinarian => veterinarian.ClinicId);
        builder.HasIndex(veterinarian => new { veterinarian.ClinicId, veterinarian.Status });
        builder.HasIndex(veterinarian => veterinarian.ExpiresAt);
    }
}
