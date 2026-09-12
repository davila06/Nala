using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Payments;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class UserPaymentProfileConfiguration : IEntityTypeConfiguration<UserPaymentProfile>
{
    public void Configure(EntityTypeBuilder<UserPaymentProfile> builder)
    {
        builder.ToTable("UserPaymentProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.MethodType).IsRequired().HasConversion<int>();
        builder.Property(x => x.ProviderToken).IsRequired().HasMaxLength(256);
        builder.Property(x => x.CardBrand).HasMaxLength(50);
        builder.Property(x => x.LastFourDigits).HasMaxLength(4);
        builder.Property(x => x.CardholderName).HasMaxLength(150);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.LastUsedAt);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
    }
}

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.PaymentProfileId);
        builder.Property(x => x.AmountCrc).HasColumnType("decimal(12,2)").IsRequired();
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(10);
        builder.Property(x => x.TransactionReference).IsRequired().HasMaxLength(100);
        builder.Property(x => x.GatewayAuthorizationCode).HasMaxLength(100);
        builder.Property(x => x.Purpose).IsRequired().HasMaxLength(50);
        builder.Property(x => x.TargetEntityId);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CompletedAt);

        builder.HasIndex(x => x.TransactionReference).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.Purpose, x.TargetEntityId });
    }
}
