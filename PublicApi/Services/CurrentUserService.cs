using System.Security.Claims;
using Domain.Abstractions;

namespace identiverse_backend.Services;

public interface ICurrentUserService
{
    CurrentUser? GetCurrentUser();
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
}

public sealed class CurrentUser
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService, ICurrentUserContext
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsAdmin
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            if (principal == null) return false;
            
            if (principal.IsInRole("Admin")) return true;
            
            var roles = principal.Claims
                .Where(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value);
            return roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
        }
    }
    
    public int UserId
    {
        get
        {
            var principal = accessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
                throw new InvalidOperationException("User is not authenticated");
            
            var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(sub, out var userId))
                throw new InvalidOperationException("Missing or invalid 'sub' claim");
            return userId;
        }
    }

    public CurrentUser? GetCurrentUser()
    {
        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(sub, out var userId))
            return null;

        var username = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        return new CurrentUser
        {
            UserId = userId,
            Username = username,
            Role = role
        };
    }

    private static string? FindFirstValueIgnoreCase(ClaimsPrincipal principal, string type)
    {
        var claim = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase));
        return claim?.Value;
    }
}