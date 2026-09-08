using AutoSale.Application.Catalog;
using AutoSale.Domain.Sales;

namespace AutoSale.Api.Contracts.Vehicles;

public sealed record AvailableVehicleResponse(Guid Id, string Make, string Model, int Year, string Color,
    decimal Price, VehicleStatus Status, int Version)
{
    public static AvailableVehicleResponse FromDto(AvailableVehicleDto vehicle) => new(
        vehicle.Id, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Color,
        vehicle.Price, vehicle.Status, vehicle.Version);
}
