using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace AutoSale.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAutoSaleAuthorization(this IServiceCollection services,
        IConfiguration configuration)
    {
        var vehiclesToSalesServiceKey = GetRequiredSecret(configuration,
            "IntegrationAuthentication:VehiclesToSalesServiceKey");
        var paymentWebhookKey = GetRequiredSecret(configuration, "IntegrationAuthentication:PaymentWebhookKey");

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => context.User
                    .FindAll(AuthorizationPolicies.CognitoGroupsClaimType)
                    .SelectMany(claim => claim.Value.Split(',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Contains(AuthorizationPolicies.AdministratorsGroup, StringComparer.Ordinal));
            })
            .AddPolicy(AuthorizationPolicies.VehiclesIntegration, policy =>
                policy.RequireAssertion(context => HasValidHeader(
                    context, AuthorizationPolicies.ServiceKeyHeader, vehiclesToSalesServiceKey)))
            .AddPolicy(AuthorizationPolicies.PaymentWebhook, policy =>
                policy.RequireAssertion(context => HasValidHeader(
                    context, AuthorizationPolicies.PaymentWebhookKeyHeader, paymentWebhookKey)));

        return services;
    }

    private static bool HasValidHeader(AuthorizationHandlerContext context, string headerName, string expected)
    {
        if (context.Resource is not HttpContext httpContext ||
            !httpContext.Request.Headers.TryGetValue(headerName, out var supplied))
        {
            return false;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(supplied.ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private static string GetRequiredSecret(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{key} must be configured.")
            : value;
    }
}
