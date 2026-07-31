using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Municipalities;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CapturedAnimalConfiguration : IEntityTypeConfiguration<CapturedAnimal>
{
    public void Configure(EntityTypeBuilder<CapturedAnimal> builder)
    {
        builder.ToTable("CapturedAnimals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Canton).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Species).IsRequired().HasMaxLength(40);
        builder.Property(x => x.Breed).HasMaxLength(80);
        builder.Property(x => x.Color).IsRequired().HasMaxLength(80);
        builder.Property(x => x.EstimatedAge).HasMaxLength(20);
        builder.Property(x => x.PhotoUrl).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CollarChipNumber).HasMaxLength(30);
        builder.Property(x => x.MatchedPetId);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CapturedAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RecordedByUserId).IsRequired();

        builder.HasIndex(x => x.Canton);
        builder.HasIndex(x => new { x.Canton, x.Status });
        builder.HasIndex(x => x.CollarChipNumber);
    }
}
