using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Api.Contracts.Catalog;

public sealed record CatalogVehicleRequest(Guid Id, string Make, string Model, int Year, string Color,
    decimal Price, VehicleStatus Status, int Version, DateTimeOffset UpdatedAtUtc)
{
    public Result<VehicleSnapshot> ToDomain() => VehicleSnapshot.Create(
        Id, Make, Model, Year, Color, Price, Status, Version, UpdatedAtUtc);
}
