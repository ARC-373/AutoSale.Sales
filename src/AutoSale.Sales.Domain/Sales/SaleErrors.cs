using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public static class SaleErrors
{
    public static readonly Error InvalidVehicleId = new("sale.vehicle_id.invalid", "Vehicle id must be specified.", ErrorType.Validation);
    public static readonly Error InvalidBuyerSubject = new("sale.buyer_subject.invalid", "Buyer subject must contain between 1 and 128 characters.", ErrorType.Validation);
    public static readonly Error InvalidBuyerCpf = new("sale.buyer_cpf.invalid", "Buyer CPF must be specified.", ErrorType.Validation);
    public static readonly Error InvalidExpectedPrice = new("sale.expected_price.invalid", "Expected price must be positive and have at most two decimal places.", ErrorType.Validation);
    public static readonly Error InvalidIdempotencyKey = new("sale.idempotency_key.invalid", "Idempotency key must contain between 1 and 100 characters.", ErrorType.Validation);
    public static readonly Error InvalidRequestHash = new("sale.request_hash.invalid", "Request hash must contain between 1 and 256 characters.", ErrorType.Validation);
    public static readonly Error InvalidVehicleSnapshot = new("sale.vehicle_snapshot.invalid", "Vehicle snapshot must belong to the sale vehicle.", ErrorType.Validation);
    public static readonly Error ReservationSnapshotNotReserved = new("sale.vehicle_snapshot.not_reserved", "The accepted reservation snapshot must have Reserved status.", ErrorType.Conflict);
    public static readonly Error InvalidFailureCode = new("sale.failure_code.invalid", "Failure code must contain between 1 and 100 characters.", ErrorType.Validation);
    public static readonly Error InvalidPaymentOutcome = new("sale.payment_outcome.invalid", "Payment outcome must be Paid or Cancelled.", ErrorType.Validation);
    public static readonly Error InvalidTimestamp = new("sale.timestamp.invalid", "Timestamp must be specified and respect the sale chronology.", ErrorType.Validation);
    public static readonly Error InvalidStateTransition = new("sale.state_transition.invalid", "The requested transition is not valid for the current sale state.", ErrorType.Conflict);
    public static readonly Error TerminalSaleCannotChange = new("sale.terminal", "A terminal sale cannot be changed.", ErrorType.Conflict);
    public static readonly Error InvalidLastError = new("sale.last_error.invalid", "Last error must contain between 1 and 2000 characters.", ErrorType.Validation);
    public static readonly Error InvalidLeaseOwner = new("sale.lease_owner.invalid", "Lease owner must contain between 1 and 128 characters.", ErrorType.Validation);
    public static readonly Error InvalidLeaseExpiration = new("sale.lease_expiration.invalid", "Lease expiration must be later than the current time.", ErrorType.Validation);
    public static readonly Error LeaseAlreadyAcquired = new("sale.lease.already_acquired", "The sale is leased by another worker.", ErrorType.Conflict);
    public static readonly Error LeaseOwnerMismatch = new("sale.lease.owner_mismatch", "Only the current lease owner can release it.", ErrorType.Conflict);
}
