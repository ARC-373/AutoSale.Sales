using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
using AutoSale.Domain.Sales;
using AutoSale.Infrastructure.BackgroundServices;
using AutoSale.SharedKernel.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutoSale.Sales.Api.UnitTests.Http;

internal sealed class ApiFactory : WebApplicationFactory<Program>
{
    internal static readonly Guid SaleId = Guid.Parse("01900000-0000-7000-8000-000000000002");
    internal static readonly Guid VehicleId = Guid.Parse("01900000-0000-7000-8000-000000000001");
    internal static readonly Guid PaymentCode = Guid.Parse("01900000-0000-7000-8000-000000000003");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sales"] = "Host=localhost;Database=api_test",
                ["IntegrationAuthentication:VehiclesServiceKey"] = "vehicles-inbound",
                ["IntegrationAuthentication:PaymentWebhookKey"] = "payment-webhook",
                ["Integrations:Vehicles:BaseUrl"] = "http://vehicles.test",
                ["Integrations:Vehicles:ServiceKey"] = "vehicles-outbound",
                ["Integrations:Payments:BaseUrl"] = "http://payments.test",
                ["Integrations:Payments:ServiceKey"] = "payments-outbound",
                ["Database:ApplyMigrationsOnStartup"] = "false"
            }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            ReplaceHandlers(services);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
                options.DefaultScheme = "Test";
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
        });
    }

    private static void ReplaceHandlers(IServiceCollection services)
    {
        Replace<ICommandHandler<PurchaseVehicleCommand, Result<SaleDto>>>(services,
            new FakeCommandHandler<PurchaseVehicleCommand, Result<SaleDto>>(_ => Result.Success(Sale())));
        Replace<IQueryHandler<GetSaleByIdQuery, Result<SaleDto>>>(services,
            new FakeQueryHandler<GetSaleByIdQuery, Result<SaleDto>>(_ => Result.Success(Sale())));
        Replace<IQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<AvailableVehicleDto>>>>(services,
            new FakeQueryHandler<ListAvailableVehiclesQuery, Result<PagedResult<AvailableVehicleDto>>>(_ =>
                Result.Success(new PagedResult<AvailableVehicleDto>(
                    [new AvailableVehicleDto(VehicleId, "Ford", "Ka", 2020, "Blue", 50_000m,
                        VehicleStatus.Available, 1)], 1, 20, 1))));
        Replace<IQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SoldVehicleDto>>>>(services,
            new FakeQueryHandler<ListSoldVehiclesQuery, Result<PagedResult<SoldVehicleDto>>>(_ =>
                Result.Success(new PagedResult<SoldVehicleDto>(
                    [new SoldVehicleDto(VehicleId, "Ford", "Ka", 2020, "Blue", 50_000m,
                        DateTimeOffset.Parse("2026-09-08T12:00:00Z"))], 1, 20, 1))));
        Replace<ICommandHandler<ReceivePaymentResultCommand, Result<PaymentResultReceipt>>>(services,
            new FakeCommandHandler<ReceivePaymentResultCommand, Result<PaymentResultReceipt>>(_ =>
                Result.Success(new PaymentResultReceipt(Sale(SaleStatus.ConfirmingVehicle), false))));
        Replace<ICommandHandler<UpsertCatalogVehicleCommand, Result<CatalogUpsertResult>>>(services,
            new FakeCommandHandler<UpsertCatalogVehicleCommand, Result<CatalogUpsertResult>>(_ =>
                Result.Success(new CatalogUpsertResult(true))));
    }

    private static void Replace<TService>(IServiceCollection services, TService implementation)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddSingleton(implementation);
    }

    private static SaleDto Sale(SaleStatus status = SaleStatus.Reserving) => new(
        SaleId, VehicleId, PaymentCode, status, null,
        DateTimeOffset.Parse("2026-09-08T12:00:00Z"), null, null, null, null, null);
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", "buyer-1"),
            new Claim("cognito:groups", "users")
        ], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
