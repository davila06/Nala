using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Safety;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class AnonymousContactRequestConfiguration
    : IEntityTypeConfiguration<AnonymousContactRequest>
{
    public void Configure(EntityTypeBuilder<AnonymousContactRequest> builder)
    {
        builder.ToTable("AnonymousContactRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FinderName).HasMaxLength(100);
        builder.Property(x => x.Message).HasMaxLength(800).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.OwnerId, x.CreatedAt });
        builder.HasIndex(x => new { x.LostPetEventId, x.CreatedAt });
    }
}