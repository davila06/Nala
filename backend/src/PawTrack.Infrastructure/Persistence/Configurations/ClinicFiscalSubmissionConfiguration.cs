using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicFiscalSubmissionConfiguration : IEntityTypeConfiguration<ClinicFiscalSubmission>
{
    public void Configure(EntityTypeBuilder<ClinicFiscalSubmission> builder)
    {
        builder.ToTable("ClinicFiscalSubmissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IssuerTaxId).HasMaxLength(12).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProviderReference).HasMaxLength(120);
        builder.HasIndex(x => x.SaleId).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.CreatedAt });
    }
}
