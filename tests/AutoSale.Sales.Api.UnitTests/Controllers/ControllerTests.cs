using AutoSale.Api.Contracts.Catalog;
using AutoSale.Api.Contracts.Payments;
using AutoSale.Api.Contracts.Sales;
using AutoSale.Api.Controllers;
using AutoSale.Application.Catalog.Upsert;
using AutoSale.Application.Common;
using AutoSale.Application.Payments.ReceiveResult;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.Purchase;
using AutoSale.Domain.Catalog;
using AutoSale.Domain.Payments;
using AutoSale.Domain.Sales;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AutoSale.Sales.Api.UnitTests.Controllers;

public sealed class ControllerTests
{
    [Fact]
    public async Task Purchase_NewSale_Returns202AndLocation()
    {
        var sale = SaleDto(SaleStatus.Reserving);
        var handler = new FakeCommandHandler<PurchaseVehicleCommand, Result<SaleDto>>(_ => Result.Success(sale));
        var controller = Prepare(new VehiclesController());

        var result = await controller.PurchaseAsync(sale.VehicleId,
            new PurchaseVehicleRequest("52998224725", 50_000m), "key-1", handler, default);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status202Accepted, objectResult.StatusCode);
        Assert.Equal($"/api/v1/sales/{sale.Id:D}", controller.Response.Headers.Location);
    }

    [Fact]
    public async Task Purchase_TerminalDuplicate_Returns200()
    {
        var sale = SaleDto(SaleStatus.Completed);
        var handler = new FakeCommandHandler<PurchaseVehicleCommand, Result<SaleDto>>(_ => Result.Success(sale));
        var controller = Prepare(new VehiclesController());

        var result = await controller.PurchaseAsync(sale.VehicleId,
            new PurchaseVehicleRequest("52998224725", 50_000m), "key-1", handler, default);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Purchase_ApplicationFailure_ReturnsProblemDetails()
    {
        var handler = new FakeCommandHandler<PurchaseVehicleCommand, Result<SaleDto>>(_ =>
            Result.Failure<SaleDto>(ApplicationErrors.IdempotencyConflict));
        var controller = Prepare(new VehiclesController());

        var result = await controller.PurchaseAsync(Guid.NewGuid(),
            new PurchaseVehicleRequest("52998224725", 50_000m), "key-1", handler, default);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Theory]
    [InlineData(false, 202)]
    [InlineData(true, 200)]
    public async Task Webhook_UsesIdempotencyStatus(bool duplicate, int expectedStatus)
    {
        var sale = SaleDto(SaleStatus.ConfirmingVehicle);
        var handler = new FakeCommandHandler<ReceivePaymentResultCommand, Result<PaymentResultReceipt>>(_ =>
            Result.Success(new PaymentResultReceipt(sale, duplicate)));
        var controller = Prepare(new PaymentsWebhookController());

        var result = await controller.ReceiveAsync(new PaymentWebhookRequest(
            sale.PaymentCode, Guid.NewGuid(), PaymentOutcome.Paid, DateTimeOffset.UtcNow), handler, default);

        var status = result.Result switch
        {
            OkObjectResult => 200,
            ObjectResult objectResult => objectResult.StatusCode,
            _ => null
        };
        Assert.Equal(expectedStatus, status);
    }

    [Fact]
    public async Task Catalog_WithRouteMismatch_Returns400WithoutCallingHandler()
    {
        var called = false;
        var handler = new FakeCommandHandler<UpsertCatalogVehicleCommand, Result<CatalogUpsertResult>>(_ =>
        {
            called = true;
            return Result.Success(new CatalogUpsertResult(true));
        });
        var request = CatalogRequest(Guid.NewGuid());

        var result = await Prepare(new CatalogIntegrationController())
            .UpsertAsync(Guid.NewGuid(), request, handler, default);

        Assert.Equal(400, Assert.IsType<ObjectResult>(result).StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task Catalog_WithValidSnapshot_Returns204()
    {
        var id = Guid.NewGuid();
        var handler = new FakeCommandHandler<UpsertCatalogVehicleCommand, Result<CatalogUpsertResult>>(_ =>
            Result.Success(new CatalogUpsertResult(true)));

        var result = await Prepare(new CatalogIntegrationController())
            .UpsertAsync(id, CatalogRequest(id), handler, default);

        Assert.IsType<NoContentResult>(result);
    }

    private static CatalogVehicleRequest CatalogRequest(Guid id) => new(
        id, "Ford", "Ka", 2020, "Blue", 50_000m, VehicleStatus.Available, 1,
        new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));

    private static SaleDto SaleDto(SaleStatus status) => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), status, status == SaleStatus.Reserving ? null : 50_000m,
        DateTimeOffset.UtcNow, null, status == SaleStatus.Completed ? DateTimeOffset.UtcNow : null,
        null, null, null);

    private static T Prepare<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.TraceIdentifier = "trace-test";
        return controller;
    }
}
