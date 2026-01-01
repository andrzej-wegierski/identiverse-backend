using System.Security.Claims;
using Domain.Exceptions;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;

namespace identiverse_backend.Authorization;

public class SelfOrAdminRequirement : IAuthorizationRequirement { }

public class SelfOrAdminHandler : AuthorizationHandler<SelfOrAdminRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAccessControlService _access;

    public SelfOrAdminHandler(IHttpContextAccessor httpContextAccessor, IAccessControlService access)
    {
        _httpContextAccessor = httpContextAccessor;
        _access = access;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SelfOrAdminRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }
        
        var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
        
        var personIdStr = routeData?.Values["id"]?.ToString() ?? routeData?.Values["personId"]?.ToString();

        if (int.TryParse(personIdStr, out var personId))
        {
            try
            {
                await _access.CanAccessPersonAsync(personId);
                context.Succeed(requirement);
            }
            catch (Exception ex) when (ex is ForbiddenException or UnauthorizedIdentiverseException or NotFoundException)
            {
                // Access denied or person not found - do not succeed
            }
        }
    }
}