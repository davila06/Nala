using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Sightings;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class SightingConfiguration : IEntityTypeConfiguration<Sighting>
{
    public void Configure(EntityTypeBuilder<Sighting> builder)
    {
        builder.ToTable("Sightings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.PetId).IsRequired();
        builder.Property(s => s.LostPetEventId);

        builder.Property(s => s.Lat).IsRequired().HasColumnType("float");
        builder.Property(s => s.Lng).IsRequired().HasColumnType("float");

        builder.Property(s => s.PhotoUrl).HasMaxLength(2048);
        builder.Property(s => s.Note).HasMaxLength(2000);

        builder.Property(s => s.SightedAt).IsRequired();
        builder.Property(s => s.ReportedAt).IsRequired();

        // Spatial geo-query index — MVP uses in-SQL BBOX filter on these two columns
        builder.HasIndex(s => new { s.Lat, s.Lng }).HasDatabaseName("IX_Sightings_LatLng");

        // Common access patterns: all sightings for a pet, linked to a lost report
        builder.HasIndex(s => s.PetId).HasDatabaseName("IX_Sightings_PetId");
        builder.HasIndex(s => s.LostPetEventId).HasDatabaseName("IX_Sightings_LostPetEventId");

        builder.Ignore(s => s.DomainEvents);
    }
}
