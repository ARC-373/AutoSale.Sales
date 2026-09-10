using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.ListSold;

public sealed class ListSoldVehiclesHandler :
    IQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SoldVehicleDto>>>
{
    private readonly ISaleRepository _saleRepository;

    public ListSoldVehiclesHandler(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<Result<PagedResult<SoldVehicleDto>>> HandleAsync(ListSoldVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<SoldVehicleDto>>(validation.Error);
        }

        return Result.Success(await _saleRepository.ListSoldAsync(query.Page, query.PageSize, cancellationToken));
    }
}
