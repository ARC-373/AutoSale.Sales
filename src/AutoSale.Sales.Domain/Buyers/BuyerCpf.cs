using AutoSale.SharedKernel.Results;

namespace AutoSale.Domain.Buyers;

public sealed class BuyerCpf : IEquatable<BuyerCpf>
{
    private const int CpfLength = 11;

    private BuyerCpf()
    {
    }

    private BuyerCpf(string value)
    {
        Value = value;
    }

    public string Value { get; private set; } = string.Empty;

    public static Result<BuyerCpf> Create(string value)
    {
        var normalized = Normalize(value);
        if (!IsValid(normalized))
        {
            return Result.Failure<BuyerCpf>(BuyerErrors.InvalidCpf);
        }

        return Result.Success(new BuyerCpf(normalized));
    }

    public bool Equals(BuyerCpf? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is BuyerCpf other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(BuyerCpf? left, BuyerCpf? right) => Equals(left, right);

    public static bool operator !=(BuyerCpf? left, BuyerCpf? right) => !Equals(left, right);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var characters = value.Trim();
        if (characters.Any(character => !char.IsDigit(character) && character is not '.' and not '-' && !char.IsWhiteSpace(character)))
        {
            return string.Empty;
        }

        return new string(characters.Where(char.IsDigit).ToArray());
    }

    private static bool IsValid(string value)
    {
        if (value.Length != CpfLength || value.All(character => character == value[0]))
        {
            return false;
        }

        return CalculateDigit(value.AsSpan(0, 9), 10) == value[9] - '0' &&
               CalculateDigit(value.AsSpan(0, 10), 11) == value[10] - '0';
    }

    private static int CalculateDigit(ReadOnlySpan<char> digits, int initialWeight)
    {
        var sum = 0;
        for (var index = 0; index < digits.Length; index++)
        {
            sum += (digits[index] - '0') * (initialWeight - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
