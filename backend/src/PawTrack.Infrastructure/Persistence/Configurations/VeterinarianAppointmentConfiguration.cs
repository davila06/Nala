using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class VeterinarianAppointmentConfiguration : IEntityTypeConfiguration<VeterinarianAppointment>
{
    public void Configure(EntityTypeBuilder<VeterinarianAppointment> builder)
    {
        builder.ToTable("VeterinarianAppointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.VeterinarianId, x.StartsAt, x.Status });
    }
}