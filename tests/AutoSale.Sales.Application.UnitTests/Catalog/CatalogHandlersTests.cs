using AutoSale.Application.Catalog.ListAvailable;
using AutoSale.Application.Catalog.Upsert;
using AutoSale.Application.Common;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Application.UnitTests.Catalog;

public sealed class CatalogHandlersTests
{
    [Fact]
    public async Task Upsert_CreatesAndThenUpdatesNewerSnapshot()
    {
        var repository = new FakeCatalogRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpsertCatalogVehicleHandler(repository, unitOfWork, new FakeClock(TestData.Now));
        var vehicleId = Guid.NewGuid();

        var created = await handler.HandleAsync(
            new UpsertCatalogVehicleCommand(vehicleId, TestData.Snapshot(vehicleId, VehicleStatus.Available)), default);
        var updated = await handler.HandleAsync(
            new UpsertCatalogVehicleCommand(vehicleId, TestData.Snapshot(vehicleId, VehicleStatus.Reserved, 2)), default);

        Assert.True(created.Value!.Applied);
        Assert.True(updated.Value!.Applied);
        Assert.Single(repository.Vehicles);
        Assert.Equal(VehicleStatus.Reserved, repository.Vehicles[0].Status);
        Assert.Equal(2, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Upsert_OlderOrEquivalentSnapshot_IsNoOp()
    {
        var vehicleId = Guid.NewGuid();
        var snapshot = TestData.Snapshot(vehicleId, VehicleStatus.Available, 2);
        var repository = new FakeCatalogRepository();
        repository.Vehicles.Add(VehicleCatalog.Create(snapshot, TestData.Now).Value!);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpsertCatalogVehicleHandler(repository, unitOfWork, new FakeClock(TestData.Now));

        var equivalent = await handler.HandleAsync(new UpsertCatalogVehicleCommand(vehicleId, snapshot), default);
        var older = await handler.HandleAsync(new UpsertCatalogVehicleCommand(
            vehicleId, TestData.Snapshot(vehicleId, VehicleStatus.Available, 1)), default);

        Assert.False(equivalent.Value!.Applied);
        Assert.False(older.Value!.Applied);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Upsert_WithMismatchedIdOrConflictingVersion_ReturnsFailure()
    {
        var vehicleId = Guid.NewGuid();
        var repository = new FakeCatalogRepository();
        repository.Vehicles.Add(VehicleCatalog.Create(
            TestData.Snapshot(vehicleId, VehicleStatus.Available), TestData.Now).Value!);
        var handler = new UpsertCatalogVehicleHandler(repository, new FakeUnitOfWork(), new FakeClock(TestData.Now));

        var mismatch = await handler.HandleAsync(new UpsertCatalogVehicleCommand(
            Guid.NewGuid(), TestData.Snapshot(vehicleId, VehicleStatus.Available)), default);
        var conflictSnapshot = VehicleSnapshot.Create(vehicleId, "Ford", "Ka", 2020, "Blue", 60_000m,
            VehicleStatus.Available, 1, TestData.Now).Value!;
        var conflict = await handler.HandleAsync(
            new UpsertCatalogVehicleCommand(vehicleId, conflictSnapshot), default);

        Assert.True(mismatch.IsFailure);
        Assert.True(conflict.IsFailure);
        Assert.Equal(VehicleCatalogErrors.SourceVersionConflict, conflict.Error);
    }

    [Fact]
    public async Task ListAvailable_ReturnsRepositoryPage()
    {
        var repository = new FakeCatalogRepository();
        var vehicleId = Guid.NewGuid();
        repository.Vehicles.Add(VehicleCatalog.Create(
            TestData.Snapshot(vehicleId, VehicleStatus.Available), TestData.Now).Value!);
        var handler = new ListAvailableVehiclesHandler(repository);

        var result = await handler.HandleAsync(new ListAvailableVehiclesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(vehicleId, result.Value.Items.Single().Id);
        Assert.Equal(1, result.Value.TotalPages);
    }

    [Fact]
    public async Task ListAvailable_WithInvalidPaging_ReturnsFailure()
    {
        var result = await new ListAvailableVehiclesHandler(new FakeCatalogRepository())
            .HandleAsync(new ListAvailableVehiclesQuery(0, 101), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.InvalidPage, result.Error);
    }
}
