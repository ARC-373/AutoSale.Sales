using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Buyers;

public static class BuyerErrors
{
    public static readonly Error InvalidCpf = new(
        "buyer.cpf.invalid",
        "CPF must be valid and contain 11 digits.",
        ErrorType.Validation);
}
