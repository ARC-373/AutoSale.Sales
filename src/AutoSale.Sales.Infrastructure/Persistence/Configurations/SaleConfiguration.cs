using AutoSale.Domain.Sales;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", table => table.HasCheckConstraint("ck_sales_price_positive", "sale_price > 0"));

        builder.HasKey(sale => sale.Id);

        builder.Property(sale => sale.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(sale => sale.VehicleId)
            .HasColumnName("vehicle_id")
            .IsRequired();

        builder.Property(sale => sale.BuyerSubject)
            .HasColumnName("buyer_subject")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(sale => sale.SalePrice)
            .HasColumnName("sale_price")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(sale => sale.PurchasedAtUtc)
            .HasColumnName("purchased_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(sale => sale.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        builder.HasOne<Vehicle>()
            .WithOne()
            .HasForeignKey<Sale>(sale => sale.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(sale => sale.VehicleId)
            .IsUnique()
            .HasDatabaseName("ux_sales_vehicle_id");

        builder.HasIndex(sale => new { sale.SalePrice, sale.Id })
            .HasDatabaseName("ix_sales_price_id");

        builder.HasIndex(sale => new { sale.BuyerSubject, sale.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ux_sales_buyer_idempotency");
    }
}
