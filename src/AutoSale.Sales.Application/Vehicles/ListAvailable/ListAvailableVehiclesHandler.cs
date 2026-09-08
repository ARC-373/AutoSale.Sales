using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Vehicles.ListAvailable;

public sealed class ListAvailableVehiclesHandler : IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<VehicleDto>>>
{
    private readonly IVehicleRepository _vehicleRepository;

    public ListAvailableVehiclesHandler(IVehicleRepository vehicleRepository)
    {
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<PagedResult<VehicleDto>>> HandleAsync(ListAvailableVehiclesQuery query, CancellationToken cancellationToken)
    {
        var validation = PagingValidator.Validate(query.Page, query.PageSize);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<VehicleDto>>(validation.Error);
        }

        var vehicles = await _vehicleRepository.ListAvailableAsync(query.Page, query.PageSize, cancellationToken);
        return Result.Success(vehicles);
    }
}
