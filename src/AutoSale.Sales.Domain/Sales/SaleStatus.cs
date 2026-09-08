namespace AutoSale.Domain.Sales;

public enum SaleStatus
{
    Reserving = 1,
    AwaitingPayment = 2,
    ConfirmingVehicle = 3,
    CancellingVehicle = 4,
    Completed = 5,
    Cancelled = 6,
    Rejected = 7
}
