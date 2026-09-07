using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Certificates;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CertificateAuditLogConfiguration : IEntityTypeConfiguration<CertificateAuditLog>
{
    public void Configure(EntityTypeBuilder<CertificateAuditLog> builder)
    {
        builder.ToTable("CertificateAuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.CertificateId).IsRequired();
        builder.Property(log => log.Action).IsRequired().HasConversion<string>().HasMaxLength(40);
        builder.Property(log => log.ActorUserId);
        builder.Property(log => log.Details).HasMaxLength(500);
        builder.Property(log => log.CreatedAt).IsRequired();

        builder.HasIndex(log => new { log.CertificateId, log.CreatedAt });
        builder.HasIndex(log => log.Action);
    }
}
