using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Pets;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.OwnerId)
            .IsRequired();

        builder.HasIndex(p => p.OwnerId); // Query pet list by owner

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Species)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Breed)
            .HasMaxLength(100);

        builder.Property(p => p.BirthDate)
            .HasColumnType("date");

        builder.Property(p => p.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // ISO 11784 microchip identifier — max 15 chars
        builder.Property(p => p.MicrochipId)
            .HasMaxLength(15);
        builder.HasIndex(p => p.MicrochipId)
            .HasFilter("[MicrochipId] IS NOT NULL")
            .IsUnique();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        // Domain events are transient — never persisted
        builder.Ignore(p => p.DomainEvents);
    }
}
