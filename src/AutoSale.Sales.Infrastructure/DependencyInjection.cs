using AutoSale.Application.Abstractions.Clock;
using AutoSale.Application.Abstractions.Integrations;
using AutoSale.Application.Abstractions.Persistence;
using AutoSale.Application.Sales.ProcessPending;
using AutoSale.Infrastructure.BackgroundServices;
using AutoSale.Infrastructure.Clock;
using AutoSale.Infrastructure.Integrations.Payments;
using AutoSale.Infrastructure.Integrations.Vehicles;
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

        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly(typeof(SalesDbContext).Assembly.FullName);
            npgsql.EnableRetryOnFailure(3);
        }));

        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IPaymentCallbackRepository, PaymentCallbackRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ProcessPendingSaleHandler>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddOptions<VehiclesOptions>()
            .BindConfiguration(VehiclesOptions.SectionName)
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "Integrations:Vehicles:BaseUrl must be an absolute URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SalesToVehiclesServiceKey),
                "Integrations:Vehicles:SalesToVehiclesServiceKey is required.")
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 60,
                "Integrations:Vehicles:TimeoutSeconds must be between 1 and 60.")
            .ValidateOnStart();
        services.AddHttpClient<IVehiclesClient, VehiclesClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<VehiclesOptions>>().Value;
            client.BaseAddress = EnsureTrailingSlash(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddOptions<PaymentsOptions>()
            .BindConfiguration(PaymentsOptions.SectionName)
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _),
                "Integrations:Payments:BaseUrl must be an absolute URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ServiceKey),
                "Integrations:Payments:ServiceKey is required.")
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 60,
                "Integrations:Payments:TimeoutSeconds must be between 1 and 60.")
            .ValidateOnStart();
        services.AddHttpClient<IPaymentProcessorClient, PaymentProcessorClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentsOptions>>().Value;
            client.BaseAddress = EnsureTrailingSlash(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddOptions<SaleProcessingOptions>()
            .BindConfiguration(SaleProcessingOptions.SectionName)
            .Validate(options => options.PollIntervalSeconds is >= 1 and <= 60,
                "SaleProcessing:PollIntervalSeconds must be between 1 and 60.")
            .Validate(options => options.LeaseSeconds is >= 5 and <= 300,
                "SaleProcessing:LeaseSeconds must be between 5 and 300.")
            .Validate(options => options.BatchSize is >= 1 and <= 100,
                "SaleProcessing:BatchSize must be between 1 and 100.")
            .ValidateOnStart();
        services.AddHostedService<SaleProcessingWorker>();

        return services;
    }

    private static Uri EnsureTrailingSlash(string baseUrl) =>
        new(baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/", UriKind.Absolute);
}
