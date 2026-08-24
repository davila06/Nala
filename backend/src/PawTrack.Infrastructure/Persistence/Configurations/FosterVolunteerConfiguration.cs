using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Fosters;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class FosterVolunteerConfiguration : IEntityTypeConfiguration<FosterVolunteer>
{
    public void Configure(EntityTypeBuilder<FosterVolunteer> builder)
    {
        builder.ToTable("FosterVolunteers");

        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.HomeLat).IsRequired();
        builder.Property(x => x.HomeLng).IsRequired();
        // JSON array replaces old CSV — backward-compat read of CSV is handled in the domain.
        builder.Property(x => x.AcceptedSpeciesJson)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnName("AcceptedSpeciesCsv"); // keep old column name so no migration is needed
        builder.Property(x => x.SizePreference).HasMaxLength(20);
        builder.Property(x => x.MaxDays).IsRequired();
        builder.Property(x => x.IsAvailable).IsRequired();
        builder.Property(x => x.AvailableUntil);
        builder.Property(x => x.TotalFostersCompleted).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.Ignore(x => x.AcceptedSpecies);

        builder.HasIndex(x => x.IsAvailable).HasDatabaseName("IX_FosterVolunteers_IsAvailable");
        builder.HasIndex(x => new { x.HomeLat, x.HomeLng }).HasDatabaseName("IX_FosterVolunteers_HomeLatLng");
    }
}
