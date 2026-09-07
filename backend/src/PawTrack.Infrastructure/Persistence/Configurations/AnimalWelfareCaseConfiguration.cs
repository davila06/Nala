using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class AnimalWelfareCaseConfiguration : IEntityTypeConfiguration<AnimalWelfareCase>
{
    public void Configure(EntityTypeBuilder<AnimalWelfareCase> builder)
    {
        builder.ToTable("AnimalWelfareCases");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.PublicCode).IsRequired().HasMaxLength(12);
        builder.Property(c => c.Type).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Severity).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Canton).IsRequired().HasMaxLength(80);
        builder.Property(c => c.DescriptionSanitized).IsRequired().HasMaxLength(2000);
        builder.Property(c => c.AssignedRole).HasMaxLength(40);
        builder.Property(c => c.ClosureReason).HasMaxLength(1000);
        builder.Ignore(c => c.IsClosed);

        builder.HasIndex(c => c.PublicCode).IsUnique();
        builder.HasIndex(c => new { c.Status, c.CreatedAt });
        builder.HasIndex(c => new { c.Severity, c.Status });
        builder.HasIndex(c => new { c.Canton, c.Status });
        builder.HasIndex(c => new { c.AssignedOrganizationUserId, c.Status });
        builder.HasIndex(c => c.PetId);
        builder.HasIndex(c => c.CapturedAnimalId);
    }
}
