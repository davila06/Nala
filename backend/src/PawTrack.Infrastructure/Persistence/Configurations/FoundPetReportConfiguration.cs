using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Sightings;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class FoundPetReportConfiguration : IEntityTypeConfiguration<FoundPetReport>
{
    public void Configure(EntityTypeBuilder<FoundPetReport> builder)
    {
        builder.ToTable("FoundPetReports");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.FoundSpecies).IsRequired();
        builder.Property(r => r.BreedEstimate).HasMaxLength(100);
        builder.Property(r => r.ColorDescription).HasMaxLength(200);
        builder.Property(r => r.SizeEstimate).HasMaxLength(50);

        builder.Property(r => r.FoundLat).IsRequired().HasColumnType("float");
        builder.Property(r => r.FoundLng).IsRequired().HasColumnType("float");

        builder.Property(r => r.PhotoUrl).HasMaxLength(2048);
        builder.Property(r => r.Note).HasMaxLength(500);

        // PII fields — stored but never exposed in public endpoints
        builder.Property(r => r.ContactName).IsRequired().HasMaxLength(100);
        builder.Property(r => r.ContactPhone).IsRequired().HasMaxLength(30);

        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.MatchedLostPetEventId);
        builder.Property(r => r.MatchScore);
        builder.Property(r => r.ReportedAt).IsRequired();

        // Indexes for common access patterns
        builder.HasIndex(r => new { r.FoundLat, r.FoundLng }).HasDatabaseName("IX_FoundPetReports_LatLng");
        builder.HasIndex(r => r.Status).HasDatabaseName("IX_FoundPetReports_Status");
        builder.HasIndex(r => r.ReportedAt).HasDatabaseName("IX_FoundPetReports_ReportedAt");

        builder.Ignore(r => r.DomainEvents);
    }
}
