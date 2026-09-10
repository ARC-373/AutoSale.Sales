using AutoSale.Application.Sales;
using AutoSale.Domain.Sales;

namespace AutoSale.Api.Contracts.Sales;

public sealed record SaleResponse(Guid Id, Guid VehicleId, Guid PaymentCode, SaleStatus Status,
    decimal? SalePrice, DateTimeOffset CreatedAtUtc, DateTimeOffset? PaymentRegisteredAtUtc,
    DateTimeOffset? CompletedAtUtc, DateTimeOffset? CancelledAtUtc, string? FailureCode,
    VehicleSnapshotResponse? VehicleSnapshot)
{
    public static SaleResponse FromDto(SaleDto sale) => new(
        sale.Id, sale.VehicleId, sale.PaymentCode, sale.Status, sale.SalePrice, sale.CreatedAtUtc,
        sale.PaymentRegisteredAtUtc, sale.CompletedAtUtc, sale.CancelledAtUtc, sale.FailureCode,
        sale.VehicleSnapshot is null ? null : VehicleSnapshotResponse.FromDto(sale.VehicleSnapshot));
}
