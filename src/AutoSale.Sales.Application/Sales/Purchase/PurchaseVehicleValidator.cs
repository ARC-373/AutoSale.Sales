using AutoSale.Domain.Buyers;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.Purchase;

public static class PurchaseVehicleValidator
{
    public static Result<BuyerCpf> Validate(PurchaseVehicleCommand command)
    {
        if (command.VehicleId == Guid.Empty)
        {
            return Result.Failure<BuyerCpf>(SaleErrors.InvalidVehicleId);
        }

        if (command.ExpectedPrice <= 0 || decimal.Round(command.ExpectedPrice, 2) != command.ExpectedPrice)
        {
            return Result.Failure<BuyerCpf>(SaleErrors.InvalidExpectedPrice);
        }

        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Trim().Length > 100)
        {
            return Result.Failure<BuyerCpf>(SaleErrors.InvalidIdempotencyKey);
        }

        return BuyerCpf.Create(command.BuyerCpf);
    }
}
