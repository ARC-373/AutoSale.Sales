using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.ListSold;

public sealed class ListSoldVehiclesHandler : IQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SaleDto>>>
{
    private readonly ISaleRepository _saleRepository;

    public ListSoldVehiclesHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<Result<PagedResult<SaleDto>>> HandleAsync(ListSoldVehiclesQuery query, CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<SaleDto>>(validation.Error);
        }

        var sales = await _saleRepository.ListSoldAsync(query.Page, query.PageSize, cancellationToken);
        return Result.Success(sales);
    }
}
