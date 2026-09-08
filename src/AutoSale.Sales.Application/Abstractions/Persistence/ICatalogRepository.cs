using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Domain.Catalog;

namespace AutoSale.Application.Abstractions.Persistence;

public interface ICatalogRepository
{
    Task<VehicleCatalog?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task AddAsync(VehicleCatalog vehicle, CancellationToken cancellationToken);
    Task<PagedResult<AvailableVehicleDto>> ListAvailableAsync(int page, int pageSize,
        CancellationToken cancellationToken);
}
