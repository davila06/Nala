using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicOrganizationMembershipConfiguration : IEntityTypeConfiguration<ClinicOrganizationMembership>
{
    public void Configure(EntityTypeBuilder<ClinicOrganizationMembership> builder)
    {
        builder.ToTable("ClinicOrganizationMemberships");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).ValueGeneratedNever();
        builder.Property(membership => membership.Role).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(membership => membership.IsRevoked).IsRequired();
        builder.Property(membership => membership.GrantedAt).IsRequired();
        builder.HasIndex(membership => new { membership.OrganizationId, membership.UserId })
            .IsUnique()
            .HasFilter("[IsRevoked] = 0")
            .HasDatabaseName("IX_ClinicOrganizationMemberships_OrganizationId_UserId");
        builder.HasIndex(membership => new { membership.UserId, membership.IsRevoked })
            .HasDatabaseName("IX_ClinicOrganizationMemberships_UserId_IsRevoked");
        builder.HasOne<ClinicOrganization>()
            .WithMany()
            .HasForeignKey(membership => membership.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PawTrack.Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
