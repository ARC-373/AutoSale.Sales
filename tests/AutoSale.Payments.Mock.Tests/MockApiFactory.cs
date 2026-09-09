using AutoSale.Payments.Mock.BackgroundServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoSale.Payments.Mock.Tests;

public sealed class MockApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"payments-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Docker");
        builder.UseSetting("ConnectionStrings:Payments", $"Data Source={_databasePath}");
        builder.UseSetting("Authentication:SalesServiceKey", "sales-test-key");
        builder.UseSetting("Authentication:OperatorKey", "operator-test-key");
        builder.UseSetting("Integrations:Sales:BaseUrl", "http://sales.test/");
        builder.UseSetting("Integrations:Sales:WebhookKey", "webhook-test-key");
        builder.UseSetting("Integrations:Sales:TimeoutSeconds", "5");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddLogging(logging => logging.ClearProviders());
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // SQLite can briefly retain a native file handle while the test host is shutting down.
        }
    }
}
