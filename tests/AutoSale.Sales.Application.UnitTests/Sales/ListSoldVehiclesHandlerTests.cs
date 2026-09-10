using AutoSale.Application.Common;
using AutoSale.Application.Sales.ListSold;

namespace AutoSale.Sales.Application.UnitTests.Sales;

public sealed class ListSoldVehiclesHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidPaging_ReturnsPage()
    {
        var result = await new ListSoldVehiclesHandler(new FakeSaleRepository())
            .HandleAsync(new ListSoldVehiclesQuery(), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPageSize_ReturnsFailure()
    {
        var result = await new ListSoldVehiclesHandler(new FakeSaleRepository())
            .HandleAsync(new ListSoldVehiclesQuery(1, 101), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.InvalidPageSize, result.Error);
    }
}
