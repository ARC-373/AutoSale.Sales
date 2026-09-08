using AutoSale.SharedKernel.Results;

namespace AutoSale.Application.Common;

public static class ApplicationErrors
{
    public static readonly Error Unauthenticated = new("auth.unauthenticated", "An authenticated user is required to perform this operation.", ErrorType.Unauthorized);
    public static readonly Error InvalidVehicleId = new("vehicle.id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error VehicleNotFound = new("vehicle.not_found", "The requested vehicle was not found.", ErrorType.NotFound);
    public static readonly Error SaleNotFound = new("sale.not_found", "The requested sale was not found.", ErrorType.NotFound);
    public static readonly Error IdempotencyConflict = new("sale.idempotency.conflict", "The idempotency key was already used with a different purchase payload.", ErrorType.Conflict);
    public static readonly Error PaymentNotFound = new("payment.not_found", "The payment code was not found.", ErrorType.NotFound);
    public static readonly Error PaymentResultConflict = new("payment_result_conflict", "The payment already has a different terminal result.", ErrorType.Conflict);
    public static readonly Error PaymentEventConflict = new("payment_event.conflict", "The event id was already used for another payment.", ErrorType.Conflict);
    public static readonly Error PaymentStateConflict = new("payment.sale_state.conflict", "The payment result is incompatible with the current sale state.", ErrorType.Conflict);
    public static readonly Error InvalidPage = new("paging.page.invalid", "Page must be greater than zero.", ErrorType.Validation);
    public static readonly Error InvalidPageSize = new("paging.page_size.invalid", "Page size must be between 1 and 100.", ErrorType.Validation);
}
