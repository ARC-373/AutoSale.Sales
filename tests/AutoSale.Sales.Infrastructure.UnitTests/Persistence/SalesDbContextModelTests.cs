using AutoSale.Domain.Catalog;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;
using AutoSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoSale.Sales.Infrastructure.UnitTests.Persistence;

public sealed class SalesDbContextModelTests
{
    [Fact]
    public void Model_UsesIsolatedSalesTablesWithoutExternalForeignKeys()
    {
        using var context = CreateContext();
        var relationalModel = context.Model.GetRelationalModel();

        Assert.Contains(relationalModel.Tables, table => table.Name == "sales");
        Assert.Contains(relationalModel.Tables, table => table.Name == "vehicle_catalog");
        Assert.Contains(relationalModel.Tables, table => table.Name == "payment_callbacks");
        Assert.Empty(relationalModel.Tables.SelectMany(table => table.ForeignKeyConstraints));
    }

    [Fact]
    public void Sale_HasConcurrencyAndRequiredPartialIndexes()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Sale))!;

        Assert.True(entity.FindProperty(nameof(Sale.Version))!.IsConcurrencyToken);
        Assert.Equal("numeric(14,2)", entity.FindProperty(nameof(Sale.ExpectedPrice))!.GetColumnType());
        Assert.Contains(entity.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_sales_buyer_idempotency" && index.IsUnique);
        Assert.Contains(entity.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_sales_active_vehicle" &&
            index.GetFilter() == "\"state\" NOT IN ('Cancelled', 'Rejected')");
        Assert.DoesNotContain(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType != typeof(Sale));
    }

    [Fact]
    public void CatalogAndCallback_HaveVersionAndEventUniqueness()
    {
        using var context = CreateContext();
        var catalog = context.Model.FindEntityType(typeof(VehicleCatalog))!;
        var callback = context.Model.FindEntityType(typeof(PaymentCallback))!;

        Assert.True(catalog.FindProperty(nameof(VehicleCatalog.SourceVersion))!.IsConcurrencyToken);
        Assert.Contains(callback.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_payment_callbacks_event_id" && index.IsUnique);
    }

    private static SalesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only")
            .Options;
        return new SalesDbContext(options);
    }
}
