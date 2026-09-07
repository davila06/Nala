using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicVerificationConfiguration : IEntityTypeConfiguration<ClinicVerification>
{
    public void Configure(EntityTypeBuilder<ClinicVerification> builder)
    {
        builder.ToTable("ClinicVerifications");
        builder.HasKey(verification => verification.Id);
        builder.Property(verification => verification.Id).ValueGeneratedNever();
        builder.Property(verification => verification.ClinicId).IsRequired();
        builder.Property(verification => verification.LicenseNumberSnapshot).IsRequired().HasMaxLength(50);
        builder.Property(verification => verification.DocumentUrl).HasMaxLength(500);
        builder.Property(verification => verification.SubmittedByUserId).IsRequired();
        builder.Property(verification => verification.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(verification => verification.SubmittedAt).IsRequired();
        builder.Property(verification => verification.VerifiedAt);
        builder.Property(verification => verification.VerifiedByAdminUserId);
        builder.Property(verification => verification.ReviewedByAdminUserId);
        builder.Property(verification => verification.ReviewedAt);
        builder.Property(verification => verification.ReviewNotes).HasMaxLength(500);
        builder.Property(verification => verification.ExpiresAt);
        builder.Property(verification => verification.RejectionReason).HasMaxLength(300);
        builder.Property(verification => verification.RevalidationRequestedAt);
        builder.Property(verification => verification.SupersededAt);

        builder.Ignore(verification => verification.IsActive);

        builder.HasIndex(verification => new { verification.ClinicId, verification.Status });
        builder.HasIndex(verification => verification.ExpiresAt);
        builder.HasIndex(verification => verification.SubmittedAt);
    }
}
