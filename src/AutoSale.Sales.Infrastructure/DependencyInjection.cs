using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Infrastructure.Clock;
using AutoSale.Infrastructure.Persistence;
using AutoSale.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSale.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AutoSaleDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AutoSaleDbContext).Assembly.FullName)));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
