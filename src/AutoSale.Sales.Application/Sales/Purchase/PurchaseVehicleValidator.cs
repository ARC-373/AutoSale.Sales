using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Sales.Purchase;

public static class PurchaseVehicleValidator
{
    public static Result Validate(PurchaseVehicleCommand command)
    {
        if (command.VehicleId == Guid.Empty)
        {
            return Result.Failure(SaleErrors.InvalidVehicleId);
        }

        var idempotencyKey = command.IdempotencyKey;
        return string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length <= 100
            ? Result.Success()
            : Result.Failure(SaleErrors.InvalidIdempotencyKey);
    }
}
