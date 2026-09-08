using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public static class SaleErrors
{
    public static readonly Error InvalidVehicleId = new("sale.vehicle_id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error InvalidBuyerSubject = new("sale.buyer_subject.invalid", "Buyer subject must contain between 1 and 128 characters.", ErrorType.Validation);
    public static readonly Error InvalidPrice = new("sale.price.invalid", "Sale price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error InvalidIdempotencyKey = new("sale.idempotency_key.invalid", "Idempotency key must contain between 1 and 100 characters when provided.", ErrorType.Validation);
}
