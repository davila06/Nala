using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.SearchCoordination;

namespace PawTrack.Infrastructure.Persistence.Configurations;

internal sealed class SearchLocationSharingSessionConfiguration
    : IEntityTypeConfiguration<SearchLocationSharingSession>
{
    public void Configure(EntityTypeBuilder<SearchLocationSharingSession> builder)
    {
        builder.ToTable("SearchLocationSharingSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConnectionId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.HasIndex(x => new { x.ExpiresAt, x.ExpiredAt, x.StoppedAt });
        builder.HasIndex(x => new { x.LostEventId, x.ConnectionId, x.IsPrecise });
    }
}
