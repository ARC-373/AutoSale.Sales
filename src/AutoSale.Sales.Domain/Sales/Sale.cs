using AutoSale.Domain.Buyers;
using AutoSale.Domain.Payments;
using AutoSale.SharedKernel.Domain;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Sales;

public sealed class Sale : Entity
{
    private const int BuyerSubjectMaxLength = 128;
    private const int IdempotencyKeyMaxLength = 100;
    private const int RequestHashMaxLength = 256;
    private const int FailureCodeMaxLength = 100;
    private const int LastErrorMaxLength = 2_000;
    private const int LeaseOwnerMaxLength = 128;

    private Sale()
    {
    }

    private Sale(Guid id, Guid vehicleId, string buyerSubject, BuyerCpf buyerCpf,
        decimal expectedPrice, string idempotencyKey, string requestHash, Guid paymentCode, DateTimeOffset createdAtUtc)
        : base(id)
    {
        VehicleId = vehicleId;
        BuyerSubject = buyerSubject;
        BuyerCpf = buyerCpf;
        ExpectedPrice = expectedPrice;
        IdempotencyKey = idempotencyKey;
        RequestHash = requestHash;
        PaymentCode = paymentCode;
        State = SaleStatus.Reserving;
        CreatedAtUtc = createdAtUtc;
        Version = 1;
    }

    public Guid VehicleId { get; private set; }
    public string BuyerSubject { get; private set; } = string.Empty;
    public BuyerCpf BuyerCpf { get; private set; } = null!;
    public decimal ExpectedPrice { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid PaymentCode { get; private set; }
    public VehicleSnapshot? VehicleSnapshot { get; private set; }
    public decimal? SalePrice { get; private set; }
    public SaleStatus State { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PaymentRegisteredAtUtc { get; private set; }
    public DateTimeOffset? PaymentOccurredAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? FailureCode { get; private set; }
    public int Version { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public bool IsTerminal => State is SaleStatus.Completed or SaleStatus.Cancelled or SaleStatus.Rejected;

    public static Result<Sale> Create(Guid vehicleId, string buyerSubject, BuyerCpf buyerCpf,
        decimal expectedPrice, string idempotencyKey, string requestHash, DateTimeOffset createdAtUtc)
    {
        if (vehicleId == Guid.Empty)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidVehicleId);
        }

        var normalizedBuyerSubject = Normalize(buyerSubject);
        if (normalizedBuyerSubject is null || normalizedBuyerSubject.Length > BuyerSubjectMaxLength)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidBuyerSubject);
        }

        if (buyerCpf is null)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidBuyerCpf);
        }

        if (expectedPrice <= 0 || decimal.Round(expectedPrice, 2) != expectedPrice)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidExpectedPrice);
        }

        var normalizedIdempotencyKey = Normalize(idempotencyKey);
        if (normalizedIdempotencyKey is null || normalizedIdempotencyKey.Length > IdempotencyKeyMaxLength)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidIdempotencyKey);
        }

        var normalizedRequestHash = Normalize(requestHash);
        if (normalizedRequestHash is null || normalizedRequestHash.Length > RequestHashMaxLength)
        {
            return Result.Failure<Sale>(SaleErrors.InvalidRequestHash);
        }

        if (!TryNormalizeTimestamp(createdAtUtc, out var normalizedCreatedAtUtc))
        {
            return Result.Failure<Sale>(SaleErrors.InvalidTimestamp);
        }

        return Result.Success(new Sale(Guid.CreateVersion7(), vehicleId, normalizedBuyerSubject, buyerCpf,
            expectedPrice, normalizedIdempotencyKey, normalizedRequestHash, Guid.CreateVersion7(), normalizedCreatedAtUtc));
    }

    public Result AcceptReservation(VehicleSnapshot snapshot)
    {
        var transition = EnsureState(SaleStatus.Reserving);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (snapshot is null || snapshot.Id != VehicleId)
        {
            return Result.Failure(SaleErrors.InvalidVehicleSnapshot);
        }

        if (snapshot.Status != VehicleStatus.Reserved)
        {
            return Result.Failure(SaleErrors.ReservationSnapshotNotReserved);
        }

        VehicleSnapshot = snapshot;
        SalePrice = snapshot.Price;
        State = SaleStatus.AwaitingPayment;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result RejectReservation(string failureCode)
    {
        var transition = EnsureState(SaleStatus.Reserving);
        if (transition.IsFailure)
        {
            return transition;
        }

        var normalizedFailureCode = Normalize(failureCode);
        if (normalizedFailureCode is null || normalizedFailureCode.Length > FailureCodeMaxLength)
        {
            return Result.Failure(SaleErrors.InvalidFailureCode);
        }

        FailureCode = normalizedFailureCode;
        State = SaleStatus.Rejected;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result RegisterPayment(DateTimeOffset registeredAtUtc)
    {
        var transition = EnsureState(SaleStatus.AwaitingPayment);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (!TryNormalizeTimestamp(registeredAtUtc, out var normalizedAtUtc) || normalizedAtUtc < CreatedAtUtc)
        {
            return Result.Failure(SaleErrors.InvalidTimestamp);
        }

        if (PaymentRegisteredAtUtc.HasValue)
        {
            return Result.Success();
        }

        PaymentRegisteredAtUtc = normalizedAtUtc;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result RecordPayment(PaymentOutcome outcome, DateTimeOffset occurredAtUtc)
    {
        var transition = EnsureState(SaleStatus.AwaitingPayment);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (!Enum.IsDefined(outcome))
        {
            return Result.Failure(SaleErrors.InvalidPaymentOutcome);
        }

        if (!TryNormalizeTimestamp(occurredAtUtc, out var normalizedAtUtc))
        {
            return Result.Failure(SaleErrors.InvalidTimestamp);
        }

        PaymentOccurredAtUtc = normalizedAtUtc;
        State = outcome == PaymentOutcome.Paid ? SaleStatus.ConfirmingVehicle : SaleStatus.CancellingVehicle;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result Complete(DateTimeOffset completedAtUtc)
    {
        var transition = EnsureState(SaleStatus.ConfirmingVehicle);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (!TryNormalizeTimestamp(completedAtUtc, out var normalizedAtUtc) ||
            (PaymentOccurredAtUtc.HasValue && normalizedAtUtc < PaymentOccurredAtUtc.Value))
        {
            return Result.Failure(SaleErrors.InvalidTimestamp);
        }

        CompletedAtUtc = normalizedAtUtc;
        State = SaleStatus.Completed;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset cancelledAtUtc)
    {
        var transition = EnsureState(SaleStatus.CancellingVehicle);
        if (transition.IsFailure)
        {
            return transition;
        }

        if (!TryNormalizeTimestamp(cancelledAtUtc, out var normalizedAtUtc) ||
            (PaymentOccurredAtUtc.HasValue && normalizedAtUtc < PaymentOccurredAtUtc.Value))
        {
            return Result.Failure(SaleErrors.InvalidTimestamp);
        }

        CancelledAtUtc = normalizedAtUtc;
        State = SaleStatus.Cancelled;
        ResetProcessingState();
        Version++;
        return Result.Success();
    }

    public Result RecordProcessingFailure(string error, DateTimeOffset nextAttemptAtUtc)
    {
        if (IsTerminal)
        {
            return Result.Failure(SaleErrors.TerminalSaleCannotChange);
        }

        var normalizedError = Normalize(error);
        if (normalizedError is null || normalizedError.Length > LastErrorMaxLength)
        {
            return Result.Failure(SaleErrors.InvalidLastError);
        }

        if (!TryNormalizeTimestamp(nextAttemptAtUtc, out var normalizedNextAttemptAtUtc))
        {
            return Result.Failure(SaleErrors.InvalidTimestamp);
        }

        Attempts++;
        LastError = normalizedError;
        NextAttemptAtUtc = normalizedNextAttemptAtUtc;
        ClearLease();
        Version++;
        return Result.Success();
    }

    public Result AcquireLease(string owner, DateTimeOffset expiresAtUtc, DateTimeOffset nowUtc)
    {
        if (IsTerminal)
        {
            return Result.Failure(SaleErrors.TerminalSaleCannotChange);
        }

        var normalizedOwner = Normalize(owner);
        if (normalizedOwner is null || normalizedOwner.Length > LeaseOwnerMaxLength)
        {
            return Result.Failure(SaleErrors.InvalidLeaseOwner);
        }

        if (!TryNormalizeTimestamp(nowUtc, out var normalizedNowUtc) ||
            !TryNormalizeTimestamp(expiresAtUtc, out var normalizedExpiresAtUtc) ||
            normalizedExpiresAtUtc <= normalizedNowUtc)
        {
            return Result.Failure(SaleErrors.InvalidLeaseExpiration);
        }

        if (LeaseExpiresAtUtc.HasValue && LeaseExpiresAtUtc.Value > normalizedNowUtc &&
            !string.Equals(LeaseOwner, normalizedOwner, StringComparison.Ordinal))
        {
            return Result.Failure(SaleErrors.LeaseAlreadyAcquired);
        }

        LeaseOwner = normalizedOwner;
        LeaseExpiresAtUtc = normalizedExpiresAtUtc;
        Version++;
        return Result.Success();
    }

    public Result ReleaseLease(string owner)
    {
        var normalizedOwner = Normalize(owner);
        if (normalizedOwner is null || !string.Equals(LeaseOwner, normalizedOwner, StringComparison.Ordinal))
        {
            return Result.Failure(SaleErrors.LeaseOwnerMismatch);
        }

        ClearLease();
        Version++;
        return Result.Success();
    }

    private Result EnsureState(SaleStatus expectedState)
    {
        if (IsTerminal)
        {
            return Result.Failure(SaleErrors.TerminalSaleCannotChange);
        }

        return State == expectedState ? Result.Success() : Result.Failure(SaleErrors.InvalidStateTransition);
    }

    private void ResetProcessingState()
    {
        Attempts = 0;
        NextAttemptAtUtc = null;
        LastError = null;
        ClearLease();
    }

    private void ClearLease()
    {
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool TryNormalizeTimestamp(DateTimeOffset value, out DateTimeOffset normalized)
    {
        normalized = value.ToUniversalTime();
        return value != default;
    }
}
