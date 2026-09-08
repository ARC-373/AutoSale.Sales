using AutoSale.Application.Sales;

namespace AutoSale.Api.Contracts.Sales;

public sealed record SaleResponse(
    Guid Id,
    Guid VehicleId,
    decimal SalePrice,
    DateTimeOffset PurchasedAtUtc,
    string? IdempotencyKey)
{
    public static SaleResponse FromDto(SaleDto sale) => new(
        sale.Id,
        sale.VehicleId,
        sale.SalePrice,
        sale.PurchasedAtUtc,
        sale.IdempotencyKey);
}
