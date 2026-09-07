using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class ServiceProviderConfiguration : IEntityTypeConfiguration<ServiceProvider>
{
    public void Configure(EntityTypeBuilder<ServiceProvider> builder)
    {
        builder.ToTable("ServiceProviders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Category).IsRequired().HasConversion<int>();
        builder.Property(x => x.Address).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Lat).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.Lng).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(120);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Website).HasMaxLength(200);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.SuspensionReason).HasMaxLength(500);
        builder.Property(x => x.IsFeatured).IsRequired();
        builder.Property(x => x.RegisteredAt).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.Category, x.IsFeatured });
    }
}

public sealed class ProviderServiceConfiguration : IEntityTypeConfiguration<ProviderService>
{
    public void Configure(EntityTypeBuilder<ProviderService> builder)
    {
        builder.ToTable("ProviderServices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceProviderId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Modality).IsRequired().HasConversion<int>();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.PriceCrc).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(x => x.Capacity).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ServiceProviderId, x.Status, x.Name });
    }
}

public sealed class ProviderBookingConfiguration : IEntityTypeConfiguration<ProviderBooking>
{
    public void Configure(EntityTypeBuilder<ProviderBooking> builder)
    {
        builder.ToTable("ProviderBookings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceProviderId).IsRequired();
        builder.Property(x => x.ProviderServiceId).IsRequired();
        builder.Property(x => x.CustomerUserId).IsRequired();
        builder.Property(x => x.PetId).IsRequired();
        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.PriceCrc).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.CustomerNote).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ProviderServiceId, x.StartsAt, x.Status });
        builder.HasIndex(x => new { x.CustomerUserId, x.CreatedAt });
        builder.HasIndex(x => new { x.ServiceProviderId, x.StartsAt });
    }
}

public sealed class ServiceAvailabilityRuleConfiguration : IEntityTypeConfiguration<ServiceAvailabilityRule>
{
    public void Configure(EntityTypeBuilder<ServiceAvailabilityRule> builder)
    {
        builder.ToTable("ServiceAvailabilityRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProviderServiceId).IsRequired();
        builder.Property(x => x.DayOfWeek).IsRequired().HasConversion<int>();
        builder.Property(x => x.StartsAtLocalTime).IsRequired();
        builder.Property(x => x.EndsAtLocalTime).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.ProviderServiceId, x.DayOfWeek, x.IsActive });
    }
}

public sealed class ServiceAvailabilityBlockConfiguration : IEntityTypeConfiguration<ServiceAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<ServiceAvailabilityBlock> builder)
    {
        builder.ToTable("ServiceAvailabilityBlocks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderServiceId).IsRequired();
        builder.Property(x => x.StartsAt).IsRequired();
        builder.Property(x => x.EndsAt).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ProviderServiceId, x.StartsAt, x.EndsAt });
    }
}

public sealed class ProviderVerificationConfiguration : IEntityTypeConfiguration<ProviderVerification>
{
    public void Configure(EntityTypeBuilder<ProviderVerification> builder)
    {
        builder.ToTable("ProviderVerifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceProviderId).IsRequired();
        builder.Property(x => x.SubmittedByUserId).IsRequired();
        builder.Property(x => x.DocumentUrl).HasMaxLength(500);
        builder.Property(x => x.DocumentContentType).HasMaxLength(100);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.SubmittedAt).IsRequired();
        builder.Property(x => x.ReviewNotes).HasMaxLength(1_000);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.SupersededAt);

        builder.HasIndex(x => new { x.ServiceProviderId, x.SubmittedAt });
        builder.HasIndex(x => new { x.Status, x.SubmittedAt });
    }
}