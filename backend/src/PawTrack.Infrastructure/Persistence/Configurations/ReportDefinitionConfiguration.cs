using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("ReportDefinitions");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.ReportType).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(80);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(160);
        builder.Property(d => d.SchemaVersion).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Scope).IsRequired().HasConversion<string>().HasMaxLength(25);
        builder.HasIndex(d => new { d.Code, d.SchemaVersion }).IsUnique();
        builder.HasIndex(d => new { d.ReportType, d.Scope, d.IsActive });
    }
}
