using AutoSale.Application.Abstractions.Messaging;
using AutoSale.Application.Common;
using AutoSale.Application.Sales;
using AutoSale.Application.Sales.ListSold;
using AutoSale.Application.Sales.Purchase;
using AutoSale.Application.Vehicles;
using AutoSale.Application.Vehicles.ListAvailable;
using AutoSale.SharedKernel.Results;

namespace AutoSale.Api.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationHandlers(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>>, PurchaseVehicleHandler>();
        services.AddScoped<IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<VehicleDto>>>, ListAvailableVehiclesHandler>();
        services.AddScoped<IQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SaleDto>>>, ListSoldVehiclesHandler>();

        return services;
    }
}
