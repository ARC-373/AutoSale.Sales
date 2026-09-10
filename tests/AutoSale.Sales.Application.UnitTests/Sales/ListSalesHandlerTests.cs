using AutoSale.Application.Sales.List;

namespace AutoSale.Sales.Application.UnitTests.Sales;

public sealed class ListSalesHandlerTests
{
    [Fact]
    public async Task Handle_returns_all_sales_ordered_by_creation_date()
    {
        var repository = new FakeSaleRepository();
        var newer = TestData.CreateSale(key: "newer");
        var older = AutoSale.Domain.Sales.Sale.Create(
            Guid.NewGuid(), "buyer-1", AutoSale.Domain.Buyers.BuyerCpf.Create("52998224725").Value!,
            50_000m, "older", "hash", TestData.Now.AddMinutes(-1)).Value!;
        repository.Sales.Add(newer);
        repository.Sales.Add(older);
        var handler = new ListSalesHandler(repository);

        var result = await handler.HandleAsync(new ListSalesQuery(1, 20), default);

        Assert.True(result.IsSuccess);
        Assert.Equal([older.Id, newer.Id], result.Value!.Items.Select(sale => sale.Id));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    public async Task Handle_rejects_invalid_paging(int page, int pageSize)
    {
        var handler = new ListSalesHandler(new FakeSaleRepository());

        var result = await handler.HandleAsync(new ListSalesQuery(page, pageSize), default);

        Assert.True(result.IsFailure);
    }
}
