using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicOrganizationSiteAccessConfiguration : IEntityTypeConfiguration<ClinicOrganizationSiteAccess>
{
    public void Configure(EntityTypeBuilder<ClinicOrganizationSiteAccess> builder)
    {
        builder.ToTable("ClinicOrganizationSiteAccess");
        builder.HasKey(access => access.Id);
        builder.Property(access => access.Id).ValueGeneratedNever();
        builder.Property(access => access.IsRevoked).IsRequired();
        builder.Property(access => access.GrantedAt).IsRequired();
        builder.HasIndex(access => new { access.UserId, access.ClinicId, access.IsRevoked })
            .HasDatabaseName("IX_ClinicOrganizationSiteAccess_UserId_ClinicId_IsRevoked");
        builder.HasIndex(access => new { access.UserId, access.ClinicId })
            .IsUnique()
            .HasFilter("[IsRevoked] = 0")
            .HasDatabaseName("IX_ClinicOrganizationSiteAccess_Active_UserId_ClinicId");
        builder.HasOne<ClinicOrganization>()
            .WithMany()
            .HasForeignKey(access => access.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(access => access.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PawTrack.Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(access => access.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PawTrack.Domain.Auth.User>()
            .WithMany()
            .HasForeignKey(access => access.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
