using System.Security.Claims;
using Domain.Exceptions;
using Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;

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
        var personIdValue = routeData?.Values["personId"] ?? routeData?.Values["id"];

        if (personIdValue is null)
        {
            context.Fail();
            return;
        }
        
        var personIdStr = personIdValue?.ToString();

        if (int.TryParse(personIdStr, out var personId))
        {
            try
            {
                await _access.CanAccessPersonAsync(personId, _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);
                context.Succeed(requirement);
            }
            catch (Exception ex) when (ex is ForbiddenException or UnauthorizedIdentiverseException or NotFoundException)
            {
                context.Fail();
            }
        }
        else
        {
            context.Fail();
        }
    }
}