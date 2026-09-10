using AutoSale.Domain.Catalog;
using AutoSale.Domain.Sales;

namespace AutoSale.Application.Catalog;

public sealed record AvailableVehicleDto(Guid Id, string Make, string Model, int Year, string Color,
    decimal Price, VehicleStatus Status, int Version)
{
    public static AvailableVehicleDto FromDomain(VehicleCatalog vehicle) => new(
        vehicle.VehicleId, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Color,
        vehicle.Price, vehicle.Status, vehicle.SourceVersion);
}
