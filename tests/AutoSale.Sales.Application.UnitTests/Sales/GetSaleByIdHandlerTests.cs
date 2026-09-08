using AutoSale.Application.Common;
using AutoSale.Application.Sales.GetById;

namespace AutoSale.Sales.Application.UnitTests.Sales;

public sealed class GetSaleByIdHandlerTests
{
    [Fact]
    public async Task HandleAsync_ForOwner_ReturnsSale()
    {
        var repository = new FakeSaleRepository();
        var sale = TestData.CreateSale();
        repository.Sales.Add(sale);

        var result = await new GetSaleByIdHandler(repository, new FakeCurrentUser("buyer-1"))
            .HandleAsync(new GetSaleByIdQuery(sale.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(sale.Id, result.Value!.Id);
    }

    [Fact]
    public async Task HandleAsync_ForAdmin_ReturnsAnotherBuyersSale()
    {
        var repository = new FakeSaleRepository();
        var sale = TestData.CreateSale();
        repository.Sales.Add(sale);

        var result = await new GetSaleByIdHandler(repository, new FakeCurrentUser("admin", true))
            .HandleAsync(new GetSaleByIdQuery(sale.Id), default);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ForUnknownOrAnotherBuyer_ReturnsNotFound(bool unknown)
    {
        var repository = new FakeSaleRepository();
        var sale = TestData.CreateSale();
        repository.Sales.Add(sale);

        var result = await new GetSaleByIdHandler(repository, new FakeCurrentUser("other"))
            .HandleAsync(new GetSaleByIdQuery(unknown ? Guid.NewGuid() : sale.Id), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.SaleNotFound, result.Error);
    }
}
