using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Fosters;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class CustodyRecordConfiguration : IEntityTypeConfiguration<CustodyRecord>
{
    public void Configure(EntityTypeBuilder<CustodyRecord> builder)
    {
        builder.ToTable("CustodyRecords");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.FosterUserId).IsRequired();
        builder.Property(x => x.FoundPetReportId).IsRequired();
        builder.Property(x => x.ExpectedDays).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Outcome).HasMaxLength(200);
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.ClosedAt);

        builder.HasIndex(x => x.FosterUserId).HasDatabaseName("IX_CustodyRecords_FosterUserId");
        builder.HasIndex(x => x.FoundPetReportId).HasDatabaseName("IX_CustodyRecords_FoundPetReportId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_CustodyRecords_Status");
    }
}
