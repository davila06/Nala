using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PawTrack.Domain.Stores;

namespace PawTrack.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Lat).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.Lng).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(120);
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.Website).HasMaxLength(200);
        builder.Property(x => x.LogoUrl).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.IsFeatured).IsRequired();
        builder.Property(x => x.RegisteredAt).IsRequired();

        builder.HasIndex(x => x.UserId).IsUnique(); // one store per user
        builder.HasIndex(x => x.Status);
    }
}

public sealed class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
{
    public void Configure(EntityTypeBuilder<StoreProduct> builder)
    {
        builder.ToTable("StoreProducts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Category).IsRequired().HasConversion<int>();
        builder.Property(x => x.PriceCrc).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(x => x.ImageUrl).HasMaxLength(500);
        builder.Property(x => x.IsAvailable).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.StoreId, x.IsAvailable });
    }
}

public sealed class StoreOrderConfiguration : IEntityTypeConfiguration<StoreOrder>
{
    public void Configure(EntityTypeBuilder<StoreOrder> builder)
    {
        builder.ToTable("StoreOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreId).IsRequired();
        builder.Property(x => x.LocationId);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.FulfillmentType).IsRequired().HasConversion<int>();
        builder.Property(x => x.PaymentReference).IsRequired().HasMaxLength(20);
        builder.Property(x => x.TotalCrc).IsRequired().HasColumnType("decimal(12,2)");
        builder.Property(x => x.DeliveryAddress).HasMaxLength(300);
        builder.Property(x => x.CustomerNote).HasMaxLength(500);
        builder.Property(x => x.StoreNote).HasMaxLength(500);
        builder.Property(x => x.PlacedAt).IsRequired();

        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        // Explicit backing-field binding — required for private readonly List<T>
        builder.Navigation(x => x.Items)
               .HasField("_items")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.PaymentReference).IsUnique();
        builder.HasIndex(x => new { x.StoreId, x.PlacedAt });
        builder.HasIndex(x => new { x.CustomerId, x.PlacedAt });
        builder.HasIndex(x => x.LocationId);
    }
}

public sealed class StoreOrderItemConfiguration : IEntityTypeConfiguration<StoreOrderItem>
{
    public void Configure(EntityTypeBuilder<StoreOrderItem> builder)
    {
        builder.ToTable("StoreOrderItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ProductName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPriceCrc).IsRequired().HasColumnType("decimal(12,2)");

        builder.Ignore(x => x.SubtotalCrc); // computed property — not persisted
    }
}

public sealed class StoreLocationConfiguration : IEntityTypeConfiguration<StoreLocation>
{
    public void Configure(EntityTypeBuilder<StoreLocation> builder)
    {
        builder.ToTable("StoreLocations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoreId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Lat).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.Lng).IsRequired().HasColumnType("decimal(9,6)");
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.StoreId, x.IsActive });
    }
}
