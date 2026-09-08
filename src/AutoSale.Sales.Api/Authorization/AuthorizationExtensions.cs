using Microsoft.AspNetCore.Authorization;

namespace AutoSale.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAutoSaleAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(context => context.User
                    .FindAll(AuthorizationPolicies.CognitoGroupsClaimType)
                    .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Contains(AuthorizationPolicies.AdministratorsGroup, StringComparer.Ordinal));
            });

        return services;
    }
}
