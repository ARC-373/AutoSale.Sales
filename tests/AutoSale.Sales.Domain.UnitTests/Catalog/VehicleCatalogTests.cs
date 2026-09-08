using AutoSale.Domain.Catalog;
using AutoSale.Domain.Sales;
using AutoSale.Sales.Domain.UnitTests.Sales;

namespace AutoSale.Sales.Domain.UnitTests.Catalog;

public sealed class VehicleCatalogTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Apply_WithNewerVersion_UpdatesProjection()
    {
        var vehicleId = Guid.NewGuid();
        var catalog = VehicleCatalog.Create(
            SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Available, 50_000m, 1), Now).Value!;
        var newerSnapshot = SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Reserved, 51_000m, 2);

        var result = catalog.Apply(newerSnapshot, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, catalog.SourceVersion);
        Assert.Equal(51_000m, catalog.Price);
        Assert.Equal(VehicleStatus.Reserved, catalog.Status);
    }

    [Fact]
    public void Apply_WithOlderVersion_IsNoOp()
    {
        var vehicleId = Guid.NewGuid();
        var current = SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Sold, 51_000m, 2);
        var catalog = VehicleCatalog.Create(current, Now).Value!;

        var result = catalog.Apply(
            SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Available, 50_000m, 1), Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, catalog.SourceVersion);
        Assert.Equal(VehicleStatus.Sold, catalog.Status);
        Assert.Equal(Now, catalog.SynchronizedAtUtc);
    }

    [Fact]
    public void Apply_WithSameVersionAndDifferentContent_ReturnsConflict()
    {
        var vehicleId = Guid.NewGuid();
        var catalog = VehicleCatalog.Create(
            SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Available, 50_000m, 1), Now).Value!;

        var result = catalog.Apply(
            SaleTests.CreateSnapshot(vehicleId, VehicleStatus.Available, 51_000m, 1), Now.AddMinutes(1));

        Assert.True(result.IsFailure);
        Assert.Equal(VehicleCatalogErrors.SourceVersionConflict, result.Error);
    }

    [Fact]
    public void Apply_WithSameVersionAndContent_IsIdempotent()
    {
        var snapshot = SaleTests.CreateSnapshot(Guid.NewGuid(), VehicleStatus.Available);
        var catalog = VehicleCatalog.Create(snapshot, Now).Value!;

        var result = catalog.Apply(snapshot, Now.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(Now, catalog.SynchronizedAtUtc);
        Assert.Equal(snapshot, catalog.ToSnapshot());
    }

    [Fact]
    public void CreateAndApply_RejectInvalidInputs()
    {
        var snapshot = SaleTests.CreateSnapshot(Guid.NewGuid(), VehicleStatus.Available);

        Assert.True(VehicleCatalog.Create(null!, Now).IsFailure);
        Assert.True(VehicleCatalog.Create(snapshot, default).IsFailure);

        var catalog = VehicleCatalog.Create(snapshot, Now).Value!;
        Assert.True(catalog.Apply(SaleTests.CreateSnapshot(Guid.NewGuid(), VehicleStatus.Available), Now).IsFailure);
        Assert.True(catalog.Apply(snapshot, default).IsFailure);
    }
}
