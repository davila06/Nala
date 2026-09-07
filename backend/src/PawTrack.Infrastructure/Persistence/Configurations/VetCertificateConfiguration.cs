using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VetCertificateConfiguration : IEntityTypeConfiguration<VetCertificate>
{
    public void Configure(EntityTypeBuilder<VetCertificate> builder)
    {
        builder.ToTable("VetCertificates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.IssuedByUserId).IsRequired();
        builder.Property(x => x.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.VerificationCode).IsRequired().HasMaxLength(8);
        builder.Property(x => x.PdfUrl).HasMaxLength(500);
        builder.Property(x => x.IssuedAt).IsRequired();
        builder.Property(x => x.ValidUntil);
        builder.Property(x => x.IsRevoked).IsRequired();
        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.RevokedByUserId);
        builder.Property(x => x.RevocationReason).HasMaxLength(300);

        builder.Ignore(x => x.IsValid); // computed

        builder.HasIndex(x => x.VerificationCode).IsUnique();
        builder.HasIndex(x => x.PetId);
        builder.HasIndex(x => new { x.ClinicId, x.IssuedAt });
    }
}
