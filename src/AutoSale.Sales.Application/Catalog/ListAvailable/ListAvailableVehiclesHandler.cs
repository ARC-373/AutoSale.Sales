using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Catalog.ListAvailable;

public sealed class ListAvailableVehiclesHandler :
    IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<AvailableVehicleDto>>>
{
    private readonly ICatalogRepository _catalogRepository;

    public ListAvailableVehiclesHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public async Task<Result<PagedResult<AvailableVehicleDto>>> HandleAsync(ListAvailableVehiclesQuery query,
        CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<AvailableVehicleDto>>(validation.Error);
        }

        return Result.Success(await _catalogRepository.ListAvailableAsync(
            query.Page, query.PageSize, cancellationToken));
    }
}
