using System.Security.Claims;
using AutoSale.Api.Authentication;
using AutoSale.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSale.Sales.Api.UnitTests.Authorization;

public sealed class AuthorizationTests
{
    [Theory]
    [InlineData(AuthorizationPolicies.VehiclesIntegration, AuthorizationPolicies.ServiceKeyHeader, "vehicles-key")]
    [InlineData(AuthorizationPolicies.PaymentWebhook, AuthorizationPolicies.PaymentWebhookKeyHeader, "payment-key")]
    public async Task IntegrationPolicies_RequireTheirOwnExactKey(string policy, string header, string key)
    {
        using var provider = CreateProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var validContext = new DefaultHttpContext();
        validContext.Request.Headers[header] = key;
        var invalidContext = new DefaultHttpContext();
        invalidContext.Request.Headers[header] = "wrong-key";

        var valid = await authorization.AuthorizeAsync(new ClaimsPrincipal(), validContext, policy);
        var invalid = await authorization.AuthorizeAsync(new ClaimsPrincipal(), invalidContext, policy);

        Assert.True(valid.Succeeded);
        Assert.False(invalid.Succeeded);
    }

    [Fact]
    public void CurrentUser_ReadsSubjectAndAdminGroup()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "buyer-1"),
                new Claim(AuthorizationPolicies.CognitoGroupsClaimType, "users,admins")
            ]))
        };
        var accessor = new HttpContextAccessor { HttpContext = context };

        var user = new CurrentUser(accessor);

        Assert.Equal("buyer-1", user.Subject);
        Assert.True(user.IsAdmin);
    }

    private static ServiceProvider CreateProvider()
    {
        var values = new Dictionary<string, string?>
        {
            ["IntegrationAuthentication:VehiclesServiceKey"] = "vehicles-key",
            ["IntegrationAuthentication:PaymentWebhookKey"] = "payment-key"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoSaleAuthorization(configuration);
        return services.BuildServiceProvider();
    }
}
