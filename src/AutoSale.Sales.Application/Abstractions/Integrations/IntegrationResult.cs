namespace AutoSale.Application.Abstractions.Integrations;

public enum IntegrationFailureKind
{
    None = 0,
    NotFound,
    Conflict,
    Transient,
    Unauthorized,
    InvalidResponse
}

public sealed record IntegrationResult<TValue>(bool IsSuccess, TValue? Value,
    IntegrationFailureKind FailureKind, string? ErrorCode, string? SanitizedError, TimeSpan? RetryAfter)
{
    public static IntegrationResult<TValue> Success(TValue value) =>
        new(true, value, IntegrationFailureKind.None, null, null, null);

    public static IntegrationResult<TValue> Failure(IntegrationFailureKind kind, string errorCode,
        string sanitizedError, TimeSpan? retryAfter = null) =>
        new(false, default, kind, errorCode, sanitizedError, retryAfter);
}
