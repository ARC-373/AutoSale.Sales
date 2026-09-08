using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Catalog;
using AutoSale.Application.Common;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly SalesDbContext _dbContext;

    public CatalogRepository(SalesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<VehicleCatalog?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        _dbContext.VehicleCatalog.SingleOrDefaultAsync(vehicle => vehicle.Id == vehicleId, cancellationToken);

    public async Task AddAsync(VehicleCatalog vehicle, CancellationToken cancellationToken)
    {
        await _dbContext.VehicleCatalog.AddAsync(vehicle, cancellationToken);
    }

    public async Task<PagedResult<AvailableVehicleDto>> ListAvailableAsync(int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.VehicleCatalog.AsNoTracking()
            .Where(vehicle => vehicle.Status == VehicleStatus.Available)
            .Where(vehicle => !_dbContext.Sales.Any(sale =>
                sale.VehicleId == vehicle.Id &&
                sale.State != SaleStatus.Cancelled && sale.State != SaleStatus.Rejected));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(vehicle => vehicle.Price).ThenBy(vehicle => vehicle.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(vehicle => new AvailableVehicleDto(vehicle.Id, vehicle.Make, vehicle.Model,
                vehicle.Year, vehicle.Color, vehicle.Price, vehicle.Status, vehicle.SourceVersion))
            .ToListAsync(cancellationToken);
        return new PagedResult<AvailableVehicleDto>(items, page, pageSize, totalCount);
    }
}
