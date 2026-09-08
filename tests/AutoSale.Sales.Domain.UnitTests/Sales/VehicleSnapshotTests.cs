using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Domain.UnitTests.Sales;

public sealed class VehicleSnapshotTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.FromHours(-3));

    [Fact]
    public void Create_NormalizesTextAndTimestamp()
    {
        var result = VehicleSnapshot.Create(Guid.NewGuid(), " Ford ", " Ka ", 2020, " Blue ",
            50_000.50m, VehicleStatus.Available, 1, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ford", result.Value!.Make);
        Assert.Equal("Ka", result.Value.Model);
        Assert.Equal("Blue", result.Value.Color);
        Assert.Equal(TimeSpan.Zero, result.Value.UpdatedAtUtc.Offset);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("make")]
    [InlineData("model")]
    [InlineData("year")]
    [InlineData("color")]
    [InlineData("price")]
    [InlineData("status")]
    [InlineData("version")]
    [InlineData("timestamp")]
    public void Create_RejectsInvalidValues(string invalidField)
    {
        var result = VehicleSnapshot.Create(
            invalidField == "id" ? Guid.Empty : Guid.NewGuid(),
            invalidField == "make" ? " " : "Ford",
            invalidField == "model" ? " " : "Ka",
            invalidField == "year" ? 1800 : 2020,
            invalidField == "color" ? " " : "Blue",
            invalidField == "price" ? 0 : 50_000m,
            invalidField == "status" ? (VehicleStatus)999 : VehicleStatus.Available,
            invalidField == "version" ? 0 : 1,
            invalidField == "timestamp" ? default : Now);

        Assert.True(result.IsFailure);
    }
}
