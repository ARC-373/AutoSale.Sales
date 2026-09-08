using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly SalesDbContext _dbContext;

    public SaleRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Sales.SingleOrDefaultAsync(sale => sale.Id == id, cancellationToken);

    public async Task<Sale?> GetByPaymentCodeForUpdateAsync(Guid paymentCode,
        CancellationToken cancellationToken)
    {
        var saleId = await _dbContext.Database.SqlQuery<Guid>($"""
                SELECT id AS "Value"
                FROM sales
                WHERE payment_code = {paymentCode}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
        return saleId == Guid.Empty
            ? null
            : await _dbContext.Sales.SingleAsync(sale => sale.Id == saleId, cancellationToken);
    }

    public Task<Sale?> GetByBuyerAndIdempotencyKeyAsync(string buyerSubject, string idempotencyKey,
        CancellationToken cancellationToken) => _dbContext.Sales.SingleOrDefaultAsync(
        sale => sale.BuyerSubject == buyerSubject && sale.IdempotencyKey == idempotencyKey,
        cancellationToken);

    public async Task<IReadOnlyCollection<Sale>> ClaimPendingAsync(string leaseOwner, DateTimeOffset nowUtc,
        DateTimeOffset leaseExpiresAtUtc, int batchSize, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var saleIds = await _dbContext.Database.SqlQuery<Guid>($"""
                SELECT id AS "Value"
                FROM sales
                WHERE state IN ('Reserving', 'AwaitingPayment', 'ConfirmingVehicle', 'CancellingVehicle')
                  AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {nowUtc})
                  AND (lease_expires_at_utc IS NULL OR lease_expires_at_utc <= {nowUtc})
                  AND NOT (state = 'AwaitingPayment' AND payment_registered_at_utc IS NOT NULL)
                ORDER BY created_at_utc, id
                FOR UPDATE SKIP LOCKED
                LIMIT {batchSize}
                """)
            .ToListAsync(cancellationToken);
        var sales = await _dbContext.Sales.Where(sale => saleIds.Contains(sale.Id))
            .OrderBy(sale => sale.CreatedAtUtc).ThenBy(sale => sale.Id)
            .ToListAsync(cancellationToken);

        foreach (var sale in sales)
        {
            var lease = sale.AcquireLease(leaseOwner, leaseExpiresAtUtc, nowUtc);
            if (lease.IsFailure)
            {
                throw new InvalidOperationException(lease.Error.Description);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return sales;
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _dbContext.Sales.AddAsync(sale, cancellationToken);
    }

    public async Task<PagedResult<SoldVehicleDto>> ListSoldAsync(int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Sales.AsNoTracking().Where(sale => sale.State == SaleStatus.Completed);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(sale => sale.SalePrice).ThenBy(sale => sale.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(sale => new SoldVehicleDto(
                sale.VehicleId,
                sale.VehicleSnapshot!.Make,
                sale.VehicleSnapshot.Model,
                sale.VehicleSnapshot.Year,
                sale.VehicleSnapshot.Color,
                sale.SalePrice!.Value,
                sale.CompletedAtUtc!.Value))
            .ToListAsync(cancellationToken);
        return new PagedResult<SoldVehicleDto>(items, page, pageSize, totalCount);
    }
}
