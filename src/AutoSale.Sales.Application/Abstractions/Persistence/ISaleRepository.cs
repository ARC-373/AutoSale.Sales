using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;

namespace AutoSale.Application.Abstractions.Persistence;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken);

    Task<PagedResult<SaleDto>> ListSoldAsync(int page, int pageSize, CancellationToken cancellationToken);
}
