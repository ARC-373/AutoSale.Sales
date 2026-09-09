namespace AutoSale.Payments.Mock.Payments;

public sealed class Payment
{
    private Payment()
    {
    }

    private Payment(Guid paymentCode, Guid saleId, decimal amount, DateTimeOffset createdAtUtc)
    {
        PaymentCode = paymentCode;
        SaleId = saleId;
        Amount = amount;
        Currency = "BRL";
        Status = PaymentStatus.Pending;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid PaymentCode { get; private set; }
    public Guid SaleId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public PaymentStatus Status { get; private set; }
    public Guid? EventId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? OccurredAtUtc { get; private set; }
    public DateTimeOffset? CallbackDeliveredAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public string? LeaseOwner { get; private set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }
    public int Version { get; private set; }

    public static Payment Create(Guid paymentCode, Guid saleId, decimal amount, string currency,
        DateTimeOffset now)
    {
        if (paymentCode == Guid.Empty || saleId == Guid.Empty)
        {
            throw new ArgumentException("Payment code and sale id are required.");
        }

        if (amount <= 0 || decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Amount must be positive and have at most two decimal places.");
        }

        if (!string.Equals(currency, "BRL", StringComparison.Ordinal))
        {
            throw new ArgumentException("Currency must be BRL.");
        }

        return new Payment(paymentCode, saleId, amount, now.ToUniversalTime());
    }

    public bool HasSameCreation(Guid saleId, decimal amount, string currency) =>
        SaleId == saleId && Amount == amount && string.Equals(Currency, currency, StringComparison.Ordinal);

    public bool Decide(PaymentStatus decision, DateTimeOffset now)
    {
        if (decision == PaymentStatus.Pending)
        {
            throw new ArgumentException("A terminal decision is required.");
        }

        if (Status == decision)
        {
            return false;
        }

        if (Status != PaymentStatus.Pending)
        {
            throw new PaymentConflictException("payment_decision_conflict",
                "The payment already has the opposite terminal decision.");
        }

        Status = decision;
        EventId = Guid.NewGuid();
        OccurredAtUtc = now.ToUniversalTime();
        NextAttemptAtUtc = now.ToUniversalTime();
        LastError = null;
        Version++;
        return true;
    }

    public void ScheduleRetry(DateTimeOffset now)
    {
        if (Status == PaymentStatus.Pending)
        {
            throw new PaymentConflictException("payment_pending", "A pending payment has no callback to retry.");
        }

        if (CallbackDeliveredAtUtc is not null)
        {
            throw new PaymentConflictException("callback_already_delivered", "The callback was already delivered.");
        }

        NextAttemptAtUtc = now.ToUniversalTime();
        LastError = null;
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
        Version++;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        CallbackDeliveredAtUtc ??= now.ToUniversalTime();
        NextAttemptAtUtc = null;
        LastError = null;
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
        Version++;
    }

    public void RecordDeliveryFailure(string error, DateTimeOffset nextAttemptAtUtc)
    {
        Attempts++;
        LastError = string.IsNullOrWhiteSpace(error) ? "Callback delivery failed." : error[..Math.Min(error.Length, 500)];
        NextAttemptAtUtc = nextAttemptAtUtc.ToUniversalTime();
        LeaseOwner = null;
        LeaseExpiresAtUtc = null;
        Version++;
    }

    public void Claim(string owner, DateTimeOffset expiresAtUtc)
    {
        LeaseOwner = owner;
        LeaseExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        Version++;
    }
}

public sealed class PaymentConflictException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
