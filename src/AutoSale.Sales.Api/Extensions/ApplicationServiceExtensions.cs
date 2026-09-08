using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Catalog;
using AutoSale.Application.Catalog.ListAvailable;
using AutoSale.Application.Catalog.Upsert;
using AutoSale.Application.Common;
using AutoSale.Application.Payments.ReceiveResult;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.GetById;
using AutoSale.Application.Sales.ListSold;
using AutoSale.Application.Sales.Purchase;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>>, PurchaseVehicleHandler>();
        services.AddScoped<IQueryHandler<GetSaleByIdQuery, Result<SaleDto>>, GetSaleByIdHandler>();
        services.AddScoped<IQueryHandler<ListAvailableVehiclesQuery,
            Result<PagedResult<AvailableVehicleDto>>>, ListAvailableVehiclesHandler>();
        services.AddScoped<IQueryHandler<ListSoldVehiclesQuery,
            Result<PagedResult<SoldVehicleDto>>>, ListSoldVehiclesHandler>();
        services.AddScoped<ICommandHandler<UpsertCatalogVehicleCommand,
            Result<CatalogUpsertResult>>, UpsertCatalogVehicleHandler>();
        services.AddScoped<ICommandHandler<ReceivePaymentResultCommand,
            Result<PaymentResultReceipt>>, ReceivePaymentResultHandler>();
        return services;
    }
}
