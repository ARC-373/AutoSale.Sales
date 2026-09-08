using System.Data;
using AutoSale.Application.Abstractions.Authentication;
using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Application.UnitTests;

internal sealed class FakeClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;
}

internal sealed class FakeCurrentUser(string? subject, bool isAdmin = false) : ICurrentUser
{
    public string? Subject { get; } = subject;
    public bool IsAdmin { get; } = isAdmin;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public FakeTransaction Transaction { get; } = new();

    public Task<ITransaction> BeginTransactionAsync(IsolationLevel isolationLevel,
        CancellationToken cancellationToken) => Task.FromResult<ITransaction>(Transaction);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTransaction : ITransaction
{
    public bool Committed { get; private set; }
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Committed = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeSaleRepository : ISaleRepository
{
    public List<Sale> Sales { get; } = [];

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Sales.SingleOrDefault(sale => sale.Id == id));

    public Task<Sale?> GetByPaymentCodeForUpdateAsync(Guid paymentCode, CancellationToken cancellationToken) =>
        Task.FromResult(Sales.SingleOrDefault(sale => sale.PaymentCode == paymentCode));

    public Task<Sale?> GetByBuyerAndIdempotencyKeyAsync(string buyerSubject, string idempotencyKey,
        CancellationToken cancellationToken) => Task.FromResult(Sales.SingleOrDefault(sale =>
            sale.BuyerSubject == buyerSubject && sale.IdempotencyKey == idempotencyKey));

    public Task<IReadOnlyCollection<Sale>> ClaimPendingAsync(string leaseOwner, DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc, int batchSize, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Sale>>(Sales.Where(sale => !sale.IsTerminal).Take(batchSize).ToArray());

    public Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        Sales.Add(sale);
        return Task.CompletedTask;
    }

    public Task<PagedResult<SoldVehicleDto>> ListSoldAsync(int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var sold = Sales.Where(sale => sale.State == SaleStatus.Completed)
            .Select(SoldVehicleDto.FromDomain).ToArray();
        return Task.FromResult(new PagedResult<SoldVehicleDto>(sold, page, pageSize, sold.Length));
    }
}

internal sealed class FakeCatalogRepository : ICatalogRepository
{
    public List<VehicleCatalog> Vehicles { get; } = [];

    public Task<VehicleCatalog?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        Task.FromResult(Vehicles.SingleOrDefault(vehicle => vehicle.VehicleId == vehicleId));

    public Task AddAsync(VehicleCatalog vehicle, CancellationToken cancellationToken)
    {
        Vehicles.Add(vehicle);
        return Task.CompletedTask;
    }

    public Task<PagedResult<AvailableVehicleDto>> ListAvailableAsync(int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var available = Vehicles.Where(vehicle => vehicle.Status == VehicleStatus.Available)
            .Select(AvailableVehicleDto.FromDomain).ToArray();
        return Task.FromResult(new PagedResult<AvailableVehicleDto>(available, page, pageSize, available.Length));
    }
}

internal sealed class FakePaymentCallbackRepository : IPaymentCallbackRepository
{
    public List<PaymentCallback> Callbacks { get; } = [];
    public Task<PaymentCallback?> GetByPaymentCodeAsync(Guid paymentCode, CancellationToken cancellationToken) =>
        Task.FromResult(Callbacks.SingleOrDefault(callback => callback.PaymentCode == paymentCode));
    public Task<PaymentCallback?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        Task.FromResult(Callbacks.SingleOrDefault(callback => callback.EventId == eventId));
    public Task AddAsync(PaymentCallback callback, CancellationToken cancellationToken)
    {
        Callbacks.Add(callback);
        return Task.CompletedTask;
    }
}

internal sealed class FakeVehiclesClient : IVehiclesClient
{
    public Func<Guid, Guid, decimal, IntegrationResult<VehicleSnapshot>> Reserve { get; set; } =
        (_, _, _) => IntegrationResult<VehicleSnapshot>.Failure(
            IntegrationFailureKind.Transient, "not_configured", "Not configured");
    public Func<Guid, Guid, IntegrationResult<VehicleSnapshot>> Confirm { get; set; } =
        (_, _) => IntegrationResult<VehicleSnapshot>.Failure(
            IntegrationFailureKind.Transient, "not_configured", "Not configured");
    public Func<Guid, Guid, IntegrationResult<VehicleSnapshot>> Release { get; set; } =
        (_, _) => IntegrationResult<VehicleSnapshot>.Failure(
            IntegrationFailureKind.Transient, "not_configured", "Not configured");

    public Task<IntegrationResult<VehicleSnapshot>> ReserveAsync(Guid vehicleId, Guid saleId,
        decimal expectedPrice, CancellationToken cancellationToken) =>
        Task.FromResult(Reserve(vehicleId, saleId, expectedPrice));
    public Task<IntegrationResult<VehicleSnapshot>> ConfirmReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken) => Task.FromResult(Confirm(vehicleId, saleId));
    public Task<IntegrationResult<VehicleSnapshot>> ReleaseReservationAsync(Guid vehicleId, Guid saleId,
        CancellationToken cancellationToken) => Task.FromResult(Release(vehicleId, saleId));
}

internal sealed class FakePaymentClient : IPaymentProcessorClient
{
    public Func<Guid, Guid, decimal, IntegrationResult<bool>> Create { get; set; } =
        (_, _, _) => IntegrationResult<bool>.Success(true);
    public Task<IntegrationResult<bool>> CreatePaymentAsync(Guid paymentCode, Guid saleId, decimal amount,
        CancellationToken cancellationToken) => Task.FromResult(Create(paymentCode, saleId, amount));
}

internal static class TestData
{
    internal static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    internal static Sale CreateSale(string subject = "buyer-1", string key = "key-1")
    {
        var vehicleId = Guid.NewGuid();
        var cpf = AutoSale.Domain.Buyers.BuyerCpf.Create("52998224725").Value!;
        return Sale.Create(vehicleId, subject, cpf, 50_000m, key,
            AutoSale.Application.Sales.Purchase.PurchaseRequestHash.Create(vehicleId, cpf, 50_000m), Now).Value!;
    }

    internal static VehicleSnapshot Snapshot(Guid id, VehicleStatus status, int version = 1) =>
        VehicleSnapshot.Create(id, "Ford", "Ka", 2020, "Blue", 50_000m,
            status, version, Now).Value!;
}
