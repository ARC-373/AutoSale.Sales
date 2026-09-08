using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Application.Abstractions.Persistence;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Vehicle?> GetByIdForPurchaseAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken);

    Task<PagedResult<VehicleDto>> ListAvailableAsync(int page, int pageSize, CancellationToken cancellationToken);
}
