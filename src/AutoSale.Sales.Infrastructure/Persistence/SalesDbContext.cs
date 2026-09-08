using AutoSale.Domain.Catalog;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence;

public sealed class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<VehicleCatalog> VehicleCatalog => Set<VehicleCatalog>();
    public DbSet<PaymentCallback> PaymentCallbacks => Set<PaymentCallback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
