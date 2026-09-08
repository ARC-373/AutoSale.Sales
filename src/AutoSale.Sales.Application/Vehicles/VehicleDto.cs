using AutoSale.Domain.Vehicles;

namespace AutoSale.Application.Vehicles;

public sealed record VehicleDto(
    Guid Id,
    string Make,
    string Model,
    int Year,
    string Color,
    decimal Price,
    VehicleStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version)
{
    public static VehicleDto FromDomain(Vehicle vehicle) => new(
        vehicle.Id,
        vehicle.Make,
        vehicle.Model,
        vehicle.Year,
        vehicle.Color,
        vehicle.Price,
        vehicle.Status,
        vehicle.CreatedAtUtc,
        vehicle.UpdatedAtUtc,
        vehicle.Version);
}
