using System.Security.Claims;

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
    public int? PersonId { get; init; }
}

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsAdmin
    {
        get
        {
            var role = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
        }
    }
    
    public CurrentUser? GetCurrentUser()
    {
        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
            return null;
        
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!int.TryParse(sub, out var userId))
            return null;
        
        var username = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        int? personId = null;
        var personIdRaw = principal.FindFirstValue("personId");
        if (int.TryParse(personIdRaw, out var pid))
            personId = pid;

        return new CurrentUser
        {
            UserId = userId,
            Username = username,
            Role = role,
            PersonId = personId
        };
    }

    
    
}