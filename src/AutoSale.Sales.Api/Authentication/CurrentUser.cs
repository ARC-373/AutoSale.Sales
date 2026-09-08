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
}
