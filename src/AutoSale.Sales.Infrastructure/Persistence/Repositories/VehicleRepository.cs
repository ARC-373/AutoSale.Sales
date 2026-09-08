using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Common;
using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace AutoSale.Infrastructure.Persistence.Repositories;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly AutoSaleDbContext _dbContext;

    public VehicleRepository(AutoSaleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Vehicles.SingleOrDefaultAsync(vehicle => vehicle.Id == id, cancellationToken);

    public Task<Vehicle?> GetByIdForPurchaseAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Vehicles
            .FromSqlInterpolated($"SELECT * FROM vehicles WHERE id = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await _dbContext.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    public async Task<PagedResult<VehicleDto>> ListAvailableAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _dbContext.Vehicles
            .AsNoTracking()
            .Where(vehicle => vehicle.Status == VehicleStatus.Available);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(vehicle => vehicle.Price)
            .ThenBy(vehicle => vehicle.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(vehicle => new VehicleDto(
                vehicle.Id,
                vehicle.Make,
                vehicle.Model,
                vehicle.Year,
                vehicle.Color,
                vehicle.Price,
                vehicle.Status,
                vehicle.CreatedAtUtc,
                vehicle.UpdatedAtUtc,
                vehicle.Version))
            .ToListAsync(cancellationToken);

        return new PagedResult<VehicleDto>(items, page, pageSize, totalCount);
    }
}
