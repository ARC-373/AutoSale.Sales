using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;

namespace AutoSale.Application.Abstractions.Persistence;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Sale?> GetByPaymentCodeForUpdateAsync(Guid paymentCode, CancellationToken cancellationToken);
    Task<Sale?> GetByBuyerAndIdempotencyKeyAsync(string buyerSubject, string idempotencyKey,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sale>> ClaimPendingAsync(string leaseOwner, DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc, int batchSize, CancellationToken cancellationToken);
    Task AddAsync(Sale sale, CancellationToken cancellationToken);
    Task<PagedResult<SaleDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResult<SoldVehicleDto>> ListSoldAsync(int page, int pageSize, CancellationToken cancellationToken);
}
