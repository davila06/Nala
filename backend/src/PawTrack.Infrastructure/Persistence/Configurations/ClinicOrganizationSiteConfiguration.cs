using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicOrganizationSiteConfiguration : IEntityTypeConfiguration<ClinicOrganizationSite>
{
    public void Configure(EntityTypeBuilder<ClinicOrganizationSite> builder)
    {
        builder.ToTable("ClinicOrganizationSites");
        builder.HasKey(site => site.Id);
        builder.Property(site => site.Id).ValueGeneratedNever();
        builder.Property(site => site.IsPrimary).IsRequired();
        builder.Property(site => site.AddedAt).IsRequired();
        builder.HasIndex(site => site.ClinicId)
            .IsUnique()
            .HasDatabaseName("IX_ClinicOrganizationSites_ClinicId");
        builder.HasIndex(site => new { site.OrganizationId, site.IsPrimary })
            .HasDatabaseName("IX_ClinicOrganizationSites_OrganizationId_IsPrimary");
        builder.HasOne<ClinicOrganization>()
            .WithMany()
            .HasForeignKey(site => site.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(site => site.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
