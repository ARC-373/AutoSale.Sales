using AutoSale.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AutoSale.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAutoSaleAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var authority = configuration["Authentication:Authority"];
        var clientId = configuration["Authentication:ClientId"];

        ArgumentException.ThrowIfNullOrWhiteSpace(authority);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.TokenValidationParameters.ValidateAudience = false;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var tokenUse = context.Principal?.FindFirst("token_use")?.Value;
                        var tokenClientId = context.Principal?.FindFirst("client_id")?.Value;

                        if (!string.Equals(tokenUse, "access", StringComparison.Ordinal) ||
                            !string.Equals(tokenClientId, clientId, StringComparison.Ordinal))
                        {
                            context.Fail("The token is not an access token issued for this Cognito app client.");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
