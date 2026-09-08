using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;

namespace AutoSale.Api.Contracts.Sales;

public sealed record VehicleSnapshotResponse(Guid Id, string Make, string Model, int Year, string Color,
    decimal Price, VehicleStatus Status, int Version, DateTimeOffset UpdatedAtUtc)
{
    public static VehicleSnapshotResponse FromDto(VehicleSnapshotDto snapshot) => new(
        snapshot.Id, snapshot.Make, snapshot.Model, snapshot.Year, snapshot.Color, snapshot.Price,
        snapshot.Status, snapshot.Version, snapshot.UpdatedAtUtc);
}
