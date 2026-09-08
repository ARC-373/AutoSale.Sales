using AutoSale.Application.Vehicles;
using AutoSale.Domain.Vehicles;

namespace AutoSale.Api.Contracts.Vehicles;

public sealed record VehicleResponse(
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
    public static VehicleResponse FromDto(VehicleDto vehicle) => new(
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
