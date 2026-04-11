using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Locations;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class GeofencedAlertLogConfiguration : IEntityTypeConfiguration<GeofencedAlertLog>
{
    public void Configure(EntityTypeBuilder<GeofencedAlertLog> builder)
    {
        builder.ToTable("GeofencedAlertLogs");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.UserId).IsRequired();
        builder.Property(g => g.LostPetEventId).IsRequired();
        builder.Property(g => g.SentAt).IsRequired();

        // Composite index optimises HasBeenAlertedAsync look-ups.
        builder.HasIndex(g => new { g.UserId, g.LostPetEventId })
               .HasDatabaseName("IX_GeofencedAlertLogs_UserId_LostPetEventId");
    }
}
