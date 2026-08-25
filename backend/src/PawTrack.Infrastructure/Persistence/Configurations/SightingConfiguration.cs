using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
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

        // Shadow property — computed from Lat/Lng in DbContext.SaveChangesAsync.
        // geography type enables STDistance() radius queries in meters.
        builder.Property<Point>("Location")
            .HasColumnType("geography")
            .IsRequired(false);

        // Replace scalar lat/lng composite index with spatial index for radius queries.
        builder.HasIndex(s => new { s.Lat, s.Lng }).HasDatabaseName("IX_Sightings_LatLng");

        builder.HasIndex(s => s.PetId).HasDatabaseName("IX_Sightings_PetId");
        builder.HasIndex(s => s.LostPetEventId).HasDatabaseName("IX_Sightings_LostPetEventId");

        builder.Ignore(s => s.DomainEvents);
    }
}
