using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using Tahdeera.Application.Common.Interfaces;

namespace Tahdeera.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var idString = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? user?.FindFirstValue("sub")
                        ?? user?.FindFirstValue("uid");

            return Guid.TryParse(idString, out var guid) ? guid : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public bool IsPlatformAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;

            var claim = user.FindFirstValue("is_platform_admin");
            if (bool.TryParse(claim, out var isAdmin) && isAdmin)
                return true;

            return user.IsInRole("PlatformAdmin") || user.IsInRole("SuperAdmin");
        }
    }

    public Guid? ActiveWorkspaceId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            // 1. Check RouteValues ("workspaceId" or "workspace_id")
            if (context.Request.RouteValues.TryGetValue("workspaceId", out var routeVal) &&
                Guid.TryParse(routeVal?.ToString(), out var routeGuid))
            {
                return routeGuid;
            }

            if (context.Request.RouteValues.TryGetValue("workspace_id", out var routeSnakeVal) &&
                Guid.TryParse(routeSnakeVal?.ToString(), out var routeSnakeGuid))
            {
                return routeSnakeGuid;
            }

            if (context.GetRouteData()?.Values.TryGetValue("workspaceId", out var routeDataVal) == true &&
                Guid.TryParse(routeDataVal?.ToString(), out var routeDataGuid))
            {
                return routeDataGuid;
            }

            // 2. Check custom header X-Workspace-Id
            if (context.Request.Headers.TryGetValue("X-Workspace-Id", out var headerValue)
                && Guid.TryParse(headerValue, out var headerGuid))
            {
                return headerGuid;
            }

            // 3. Check JWT token claim workspace_id
            var claimValue = context.User?.FindFirstValue("workspace_id");
            if (Guid.TryParse(claimValue, out var claimGuid))
            {
                return claimGuid;
            }

            return null;
        }
    }
}
