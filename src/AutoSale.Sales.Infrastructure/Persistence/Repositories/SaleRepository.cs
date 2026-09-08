using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly AutoSaleDbContext _dbContext;

    public SaleRepository(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken)
    {
        await _dbContext.Sales.AddAsync(sale, cancellationToken);
    }

    public async Task<PagedResult<SaleDto>> ListSoldAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sales.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(sale => sale.SalePrice)
            .ThenBy(sale => sale.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sale => new SaleDto(
                sale.Id,
                sale.VehicleId,
                sale.BuyerSubject,
                sale.SalePrice,
                sale.PurchasedAtUtc,
                sale.IdempotencyKey))
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleDto>(items, page, pageSize, totalCount);
    }
}
