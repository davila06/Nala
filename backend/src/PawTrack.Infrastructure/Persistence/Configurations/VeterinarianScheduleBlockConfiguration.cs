using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VeterinarianScheduleBlockConfiguration : IEntityTypeConfiguration<VeterinarianScheduleBlock>
{
    public void Configure(EntityTypeBuilder<VeterinarianScheduleBlock> builder)
    {
        builder.ToTable("VeterinarianScheduleBlocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(200);
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.VeterinarianId, x.StartsAt });
    }
}
