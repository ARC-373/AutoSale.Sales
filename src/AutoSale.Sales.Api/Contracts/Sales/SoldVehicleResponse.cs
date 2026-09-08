using AutoSale.Application.Sales;

namespace AutoSale.Api.Contracts.Sales;

public sealed record SoldVehicleResponse(Guid VehicleId, string Make, string Model, int Year, string Color,
    decimal SalePrice, DateTimeOffset SoldAtUtc)
{
    public static SoldVehicleResponse FromDto(SoldVehicleDto vehicle) => new(
        vehicle.VehicleId, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Color,
        vehicle.SalePrice, vehicle.SoldAtUtc);
}
