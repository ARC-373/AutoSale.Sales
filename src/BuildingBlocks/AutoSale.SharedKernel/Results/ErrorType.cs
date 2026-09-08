namespace AutoSale.SharedKernel.Results;

public enum ErrorType
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure
}
