using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicWidgetDomainConfiguration : IEntityTypeConfiguration<ClinicWidgetDomain>
{
    public void Configure(EntityTypeBuilder<ClinicWidgetDomain> builder)
    {
        builder.ToTable("ClinicWidgetDomains");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Domain).HasMaxLength(253).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.Domain }).IsUnique();
        builder.HasIndex(x => new { x.ClinicId, x.IsActive });
    }
}
