using AutoSale.Domain.Sales;

namespace AutoSale.Application.Sales;

public sealed record SaleDto(Guid Id, Guid VehicleId, Guid PaymentCode, SaleStatus Status,
    decimal? SalePrice, DateTimeOffset CreatedAtUtc, DateTimeOffset? PaymentRegisteredAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? CancelledAtUtc, string? FailureCode,
    VehicleSnapshotDto? VehicleSnapshot)
{
    public static SaleDto FromDomain(Sale sale) => new(
        sale.Id, sale.VehicleId, sale.PaymentCode, sale.State, sale.SalePrice, sale.CreatedAtUtc,
        sale.PaymentRegisteredAtUtc, sale.CompletedAtUtc, sale.CancelledAtUtc, sale.FailureCode,
        sale.VehicleSnapshot is null ? null : VehicleSnapshotDto.FromDomain(sale.VehicleSnapshot));
}

public sealed record VehicleSnapshotDto(Guid Id, string Make, string Model, int Year, string Color,
    decimal Price, VehicleStatus Status, int Version, DateTimeOffset UpdatedAtUtc)
{
    public static VehicleSnapshotDto FromDomain(VehicleSnapshot snapshot) => new(
        snapshot.Id, snapshot.Make, snapshot.Model, snapshot.Year, snapshot.Color, snapshot.Price,
        snapshot.Status, snapshot.Version, snapshot.UpdatedAtUtc);
}
