using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicStaffMembershipConfiguration : IEntityTypeConfiguration<ClinicStaffMembership>
{
    public void Configure(EntityTypeBuilder<ClinicStaffMembership> builder)
    {
        builder.ToTable("ClinicStaffMemberships");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.Id).ValueGeneratedNever();
        builder.Property(member => member.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(member => new { member.ClinicId, member.UserId }).IsUnique();
        builder.HasIndex(member => new { member.ClinicId, member.IsRevoked });
    }
}