using AutoSale.Application.Common;
using AutoSale.Application.Sales.Purchase;
using AutoSale.Domain.Sales;

namespace AutoSale.Sales.Application.UnitTests.Sales;

public sealed class PurchaseVehicleHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesReservingSaleWithoutExternalCalls()
    {
        var repository = new FakeSaleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new PurchaseVehicleHandler(repository, unitOfWork,
            new FakeCurrentUser(" buyer-1 "), new FakeClock(TestData.Now));
        var command = new PurchaseVehicleCommand(Guid.NewGuid(), "529.982.247-25", 50_000m, " key-1 ");

        var result = await handler.HandleAsync(command, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(SaleStatus.Reserving, result.Value!.Status);
        Assert.Single(repository.Sales);
        Assert.Equal(50_000m, repository.Sales[0].ExpectedPrice);
        Assert.Equal("buyer-1", repository.Sales[0].BuyerSubject);
        Assert.Equal(1, unitOfWork.TransactionExecutionCount);
    }

    [Fact]
    public async Task HandleAsync_WithSameKeyAndPayload_ReturnsExistingSale()
    {
        var vehicleId = Guid.NewGuid();
        var repository = new FakeSaleRepository();
        var firstHandler = CreateHandler(repository, new FakeCurrentUser("buyer-1"));
        var command = new PurchaseVehicleCommand(vehicleId, "52998224725", 50_000m, "key-1");
        var first = await firstHandler.HandleAsync(command, default);

        var second = await CreateHandler(repository, new FakeCurrentUser("buyer-1"))
            .HandleAsync(command, default);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Single(repository.Sales);
    }

    [Fact]
    public async Task HandleAsync_WithSameKeyAndDifferentPayload_ReturnsConflict()
    {
        var repository = new FakeSaleRepository();
        var handler = CreateHandler(repository, new FakeCurrentUser("buyer-1"));
        await handler.HandleAsync(
            new PurchaseVehicleCommand(Guid.NewGuid(), "52998224725", 50_000m, "key-1"), default);

        var result = await handler.HandleAsync(
            new PurchaseVehicleCommand(Guid.NewGuid(), "52998224725", 50_000m, "key-1"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.IdempotencyConflict, result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithoutAuthenticatedSubject_ReturnsUnauthorized()
    {
        var result = await CreateHandler(new FakeSaleRepository(), new FakeCurrentUser(null)).HandleAsync(
            new PurchaseVehicleCommand(Guid.NewGuid(), "52998224725", 50_000m, "key-1"), default);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.Unauthenticated, result.Error);
    }

    [Theory]
    [InlineData("vehicle")]
    [InlineData("cpf")]
    [InlineData("price")]
    [InlineData("key")]
    public async Task HandleAsync_WithInvalidRequest_ReturnsValidationError(string field)
    {
        var command = new PurchaseVehicleCommand(
            field == "vehicle" ? Guid.Empty : Guid.NewGuid(),
            field == "cpf" ? "11111111111" : "52998224725",
            field == "price" ? 0 : 50_000m,
            field == "key" ? " " : "key-1");

        var result = await CreateHandler(new FakeSaleRepository(), new FakeCurrentUser("buyer-1"))
            .HandleAsync(command, default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RequestHash_NormalizesEquivalentCpfAndMoneyRepresentations()
    {
        var vehicleId = Guid.NewGuid();
        var formatted = AutoSale.Domain.Buyers.BuyerCpf.Create("529.982.247-25").Value!;
        var digits = AutoSale.Domain.Buyers.BuyerCpf.Create("52998224725").Value!;

        Assert.Equal(PurchaseRequestHash.Create(vehicleId, formatted, 50_000.0m),
            PurchaseRequestHash.Create(vehicleId, digits, 50_000.00m));
    }

    private static PurchaseVehicleHandler CreateHandler(FakeSaleRepository repository, FakeCurrentUser user) =>
        new(repository, new FakeUnitOfWork(), user, new FakeClock(TestData.Now));
}
