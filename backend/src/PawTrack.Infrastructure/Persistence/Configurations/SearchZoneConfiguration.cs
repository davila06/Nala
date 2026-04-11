using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.LostPets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class SearchZoneConfiguration : IEntityTypeConfiguration<SearchZone>
{
    public void Configure(EntityTypeBuilder<SearchZone> builder)
    {
        builder.ToTable("SearchZones");

        builder.HasKey(z => z.Id);
        builder.Property(z => z.Id).ValueGeneratedNever();

        builder.Property(z => z.LostPetEventId).IsRequired();
        builder.Property(z => z.Label).IsRequired().HasMaxLength(100);

        // GeoJSON polygons can be large (49 zones × ~250 chars each) — use MAX
        builder.Property(z => z.GeoJsonPolygon).IsRequired().HasColumnType("nvarchar(max)");

        builder.Property(z => z.Status)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(z => z.AssignedToUserId);
        builder.Property(z => z.TakenAt);
        builder.Property(z => z.ClearedAt);
        builder.Property(z => z.CreatedAt).IsRequired();

        // Efficient lookup: find all zones for a lost event
        builder.HasIndex(z => z.LostPetEventId).HasDatabaseName("IX_SearchZones_LostPetEventId");

        // Efficient lookup: find zones by status for a given event
        builder.HasIndex(z => new { z.LostPetEventId, z.Status })
               .HasDatabaseName("IX_SearchZones_LostPetEventId_Status");
    }
}
