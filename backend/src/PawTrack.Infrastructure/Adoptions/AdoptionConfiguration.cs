using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Adoptions;
using System.Text.Json;

namespace PawTrack.Infrastructure.Adoptions;

internal sealed class AdoptablePetConfiguration : IEntityTypeConfiguration<AdoptablePet>
{
    private static readonly JsonSerializerOptions _jsonOpts = new();

    public void Configure(EntityTypeBuilder<AdoptablePet> b)
    {
        b.ToTable("AdoptableAnimals");
        b.HasKey(a => a.Id);

        b.Property(a => a.Name).HasMaxLength(80).IsRequired();
        b.Property(a => a.Story).HasMaxLength(2000).IsRequired();
        b.Property(a => a.Breed).HasMaxLength(100);
        b.Property(a => a.Requirements).HasMaxLength(500);
        b.Property(a => a.MedicalNotes).HasMaxLength(500);
        b.Property(a => a.RefLabel).HasMaxLength(100);
        b.Property(a => a.RefLat).HasColumnType("decimal(9,6)");
        b.Property(a => a.RefLng).HasColumnType("decimal(9,6)");

        b.Property(a => a.Species).HasConversion<string>().HasMaxLength(20);
        b.Property(a => a.Size).HasConversion<string>().HasMaxLength(10);
        b.Property(a => a.AgeCategory).HasConversion<string>().HasMaxLength(10);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(15);

        // Private backing field serialised as JSON array
        b.Property<List<string>>("_photoUrls")
            .HasField("_photoUrls")
            .HasColumnName("PhotoUrls")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOpts),
                v => JsonSerializer.Deserialize<List<string>>(v, _jsonOpts) ?? new());

        b.HasIndex(a => a.OrganizationUserId);
        b.HasIndex(a => a.Status);
        b.HasIndex(a => new { a.Species, a.Status });
    }
}

internal sealed class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> b)
    {
        b.ToTable("AdoptionApplications");
        b.HasKey(a => a.Id);

        b.Property(a => a.ApplicantNote).HasMaxLength(500).IsRequired();
        b.Property(a => a.ReviewNote).HasMaxLength(300);
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(15);

        b.HasIndex(a => a.AdoptablePetId);
        b.HasIndex(a => a.ApplicantUserId);
        // One non-withdrawn application per applicant per animal enforced in app layer
        b.HasIndex(a => new { a.ApplicantUserId, a.AdoptablePetId });
    }
}

internal sealed class AdoptionFairConfiguration : IEntityTypeConfiguration<AdoptionFair>
{
    private static readonly JsonSerializerOptions _jsonOpts = new();

    public void Configure(EntityTypeBuilder<AdoptionFair> b)
    {
        b.ToTable("AdoptionFairs");
        b.HasKey(f => f.Id);

        b.Property(f => f.Title).HasMaxLength(150).IsRequired();
        b.Property(f => f.Description).HasMaxLength(1000);
        b.Property(f => f.VenueLabel).HasMaxLength(200).IsRequired();
        b.Property(f => f.Status).HasConversion<string>().HasMaxLength(12);

        // Private backing field serialised as JSON array
        b.Property<List<Guid>>("_animalIds")
            .HasField("_animalIds")
            .HasColumnName("AnimalIds")
            .HasColumnType("nvarchar(max)")
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOpts),
                v => JsonSerializer.Deserialize<List<Guid>>(v, _jsonOpts) ?? new());

        b.HasIndex(f => f.OrganizationUserId);
        b.HasIndex(f => f.Status);
        b.HasIndex(f => f.StartsAt);
    }
}
