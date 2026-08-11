using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class HealthProtocolConfiguration : IEntityTypeConfiguration<HealthProtocol>
{
    public void Configure(EntityTypeBuilder<HealthProtocol> builder)
    {
        builder.ToTable("HealthProtocols");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Species).IsRequired().HasMaxLength(20);
        builder.Property(x => x.RecordType).IsRequired().HasConversion<int>();
        builder.Property(x => x.ProtocolName).IsRequired().HasMaxLength(120);
        builder.Property(x => x.IntervalDays).IsRequired();

        builder.HasIndex(x => new { x.Species, x.RecordType });

        // Seed CR veterinary protocols at migration time
        builder.HasData(HealthProtocol.SeedData());
    }
}
