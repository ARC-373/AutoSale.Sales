using AutoSale.Domain.Buyers;

namespace AutoSale.Sales.Domain.UnitTests.Buyers;

public sealed class BuyerCpfTests
{
    [Theory]
    [InlineData("52998224725", "52998224725")]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 529.982.247-25 ", "52998224725")]
    public void Create_WithValidCpf_NormalizesValue(string input, string expected)
    {
        var result = BuyerCpf.Create(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("11111111111")]
    [InlineData("52998224724")]
    [InlineData("abc52998224725")]
    public void Create_WithInvalidCpf_ReturnsFailure(string input)
    {
        var result = BuyerCpf.Create(input);

        Assert.True(result.IsFailure);
        Assert.Equal(BuyerErrors.InvalidCpf, result.Error);
    }

    [Fact]
    public void Equality_UsesNormalizedValue()
    {
        var formatted = BuyerCpf.Create("529.982.247-25").Value!;
        var digitsOnly = BuyerCpf.Create("52998224725").Value!;

        Assert.Equal(formatted, digitsOnly);
        Assert.True(formatted == digitsOnly);
    }
}
