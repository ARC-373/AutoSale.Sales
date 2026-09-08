using System.Security.Claims;
using AutoSale.Application.Abstractions.Authentication;

namespace AutoSale.Api.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Subject => _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User
        .FindAll(Authorization.AuthorizationPolicies.CognitoGroupsClaimType)
        .SelectMany(claim => claim.Value.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Contains(Authorization.AuthorizationPolicies.AdministratorsGroup, StringComparer.Ordinal) == true;
}
