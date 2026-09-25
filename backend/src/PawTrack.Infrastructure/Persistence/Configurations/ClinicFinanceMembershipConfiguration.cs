using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicFinanceMembershipConfiguration : IEntityTypeConfiguration<ClinicFinanceMembership>
{
    public void Configure(EntityTypeBuilder<ClinicFinanceMembership> builder)
    {
        builder.ToTable("ClinicFinanceMemberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.ClinicId, x.UserId }).IsUnique();
    }
}
