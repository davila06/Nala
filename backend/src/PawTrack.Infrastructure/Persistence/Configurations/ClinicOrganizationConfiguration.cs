using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Clinics;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ClinicOrganizationConfiguration : IEntityTypeConfiguration<ClinicOrganization>
{
    public void Configure(EntityTypeBuilder<ClinicOrganization> builder)
    {
        builder.ToTable("ClinicOrganizations");
        builder.HasKey(organization => organization.Id);
        builder.Property(organization => organization.Id).ValueGeneratedNever();
        builder.Property(organization => organization.Name).IsRequired().HasMaxLength(200);
        builder.Property(organization => organization.CreatedAt).IsRequired();

        builder.Ignore(organization => organization.Memberships);
        builder.Ignore(organization => organization.Sites);
    }
}
