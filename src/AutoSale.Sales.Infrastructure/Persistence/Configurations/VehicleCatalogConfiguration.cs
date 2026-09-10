using AutoSale.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoSale.Infrastructure.Persistence.Configurations;

public sealed class VehicleCatalogConfiguration : IEntityTypeConfiguration<VehicleCatalog>
{
    public void Configure(EntityTypeBuilder<VehicleCatalog> builder)
    {
        builder.ToTable("vehicle_catalog");
        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasColumnName("vehicle_id").ValueGeneratedNever();
        builder.Ignore(vehicle => vehicle.VehicleId);
        builder.Property(vehicle => vehicle.Make).HasColumnName("make").HasMaxLength(120).IsRequired();
        builder.Property(vehicle => vehicle.Model).HasColumnName("model").HasMaxLength(120).IsRequired();
        builder.Property(vehicle => vehicle.Year).HasColumnName("year").IsRequired();
        builder.Property(vehicle => vehicle.Color).HasColumnName("color").HasMaxLength(50).IsRequired();
        builder.Property(vehicle => vehicle.Price).HasColumnName("price").HasPrecision(14, 2).IsRequired();
        builder.Property(vehicle => vehicle.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(vehicle => vehicle.SourceVersion).HasColumnName("source_version").IsConcurrencyToken().IsRequired();
        builder.Property(vehicle => vehicle.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc").IsRequired();
        builder.Property(vehicle => vehicle.SynchronizedAtUtc).HasColumnName("synchronized_at_utc").IsRequired();
        builder.HasIndex(vehicle => new { vehicle.Status, vehicle.Price, vehicle.Id })
            .HasDatabaseName("ix_vehicle_catalog_status_price_id");
    }
}
