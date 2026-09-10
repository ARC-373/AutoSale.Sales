using System.Text.Json.Serialization;
using AutoSale.Payments.Mock.Authentication;
using AutoSale.Payments.Mock.BackgroundServices;
using AutoSale.Payments.Mock.Integrations;
using AutoSale.Payments.Mock.Middleware;
using AutoSale.Payments.Mock.Payments;
using AutoSale.Payments.Mock.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Payments")
    ?? throw new InvalidOperationException("ConnectionStrings:Payments must be configured.");
var salesServiceKey = RequiredSecret(builder.Configuration, "Authentication:SalesServiceKey");
var operatorKey = RequiredSecret(builder.Configuration, "Authentication:OperatorKey");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandlingMiddleware>();
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation",
            Detail = "The request body or parameters are invalid.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = "request.invalid";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddOpenApi();
builder.Services.AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeySchemes.Sales, options =>
    {
        options.HeaderName = "X-Service-Key";
        options.ApiKey = salesServiceKey;
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeySchemes.Operator, options =>
    {
        options.HeaderName = "X-Operator-Key";
        options.ApiKey = operatorKey;
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(ApiKeySchemes.SalesPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(ApiKeySchemes.Sales);
        policy.RequireAuthenticatedUser();
    })
    .AddPolicy(ApiKeySchemes.OperatorPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(ApiKeySchemes.Operator);
        policy.RequireAuthenticatedUser();
    });
builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<SalesWebhookOptions>()
    .BindConfiguration(SalesWebhookOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHttpClient<SalesWebhookClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<SalesWebhookOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});
builder.Services.AddHostedService<CallbackDeliveryWorker>();
builder.Services.AddHealthChecks().AddDbContextCheck<PaymentsDbContext>("sqlite");

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapOpenApi();
app.MapScalarApiReference("/docs", options => options.WithTitle("AutoSale Payments Mock"));

app.Run();

static string RequiredSecret(IConfiguration configuration, string key) =>
    string.IsNullOrWhiteSpace(configuration[key])
        ? throw new InvalidOperationException($"{key} must be configured.")
        : configuration[key]!;

public partial class Program;
