using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public sealed class Sale : Entity
{
    private const int BuyerSubjectMaxLength = 128;
    private const int IdempotencyKeyMaxLength = 100;

    private Sale()
    {
    }

    private Sale(Guid id, Guid vehicleId, string buyerSubject, decimal salePrice, DateTimeOffset purchasedAtUtc, string? idempotencyKey)
        : base(id)
    {
        VehicleId = vehicleId;
        BuyerSubject = buyerSubject;
        SalePrice = salePrice;
        PurchasedAtUtc = purchasedAtUtc;
        IdempotencyKey = idempotencyKey;
    }

    public Guid VehicleId { get; private set; }

    public string BuyerSubject { get; private set; } = string.Empty;

    public decimal SalePrice { get; private set; }

    public DateTimeOffset PurchasedAtUtc { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public static Result<Sale> Create(
        Guid vehicleId,
        string buyerSubject,
        decimal salePrice,
        DateTimeOffset purchasedAt,
        string? idempotencyKey = null)
    {
        if (vehicleId == Guid.Empty)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidVehicleId);
        }

        var normalizedBuyerSubject = buyerSubject?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBuyerSubject) || normalizedBuyerSubject.Length > BuyerSubjectMaxLength)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidBuyerSubject);
        }

        if (salePrice <= 0 || decimal.Round(salePrice, 2) != salePrice)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidPrice);
        }

        var normalizedIdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (normalizedIdempotencyKey is not null && normalizedIdempotencyKey.Length > IdempotencyKeyMaxLength)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidIdempotencyKey);
        }

        return Result.Success(new Sale(
            Guid.CreateVersion7(),
            vehicleId,
            normalizedBuyerSubject,
            salePrice,
            purchasedAt.ToUniversalTime(),
            normalizedIdempotencyKey));
    }
}
