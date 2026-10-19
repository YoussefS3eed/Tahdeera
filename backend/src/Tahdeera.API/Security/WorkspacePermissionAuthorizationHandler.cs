using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tahdeera.Application.Common.Security;
using Tahdeera.Domain.Repositories;

namespace Tahdeera.API.Security;

public class WorkspacePermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public WorkspacePermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// Enforces Zero-Trust server-side Layer 2 (workspace membership) and Layer 4 (RBAC matrix)
/// authorization on all protected workspace endpoints per Constitution Principle 3 and AUTHORIZATION_MODEL.md.
/// </summary>
public class WorkspacePermissionAuthorizationHandler : AuthorizationHandler<WorkspacePermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public WorkspacePermissionAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WorkspacePermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // 1. PlatformAdmin / SuperAdmin passes all workspace permission checks
        if (context.User.IsInRole("PlatformAdmin") ||
            context.User.IsInRole("SuperAdmin") ||
            context.User.HasClaim(c => c.Type == "is_platform_admin" && c.Value.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Direct permission claim override (if granted via fine-grained token)
        if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // 3. Extract workspaceId from Route or Header
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        Guid workspaceId = Guid.Empty;
        if (httpContext.Request.RouteValues.TryGetValue("workspaceId", out var routeVal) &&
            Guid.TryParse(routeVal?.ToString(), out var parsedRouteId))
        {
            workspaceId = parsedRouteId;
        }
        else if (httpContext.Request.Headers.TryGetValue("X-Workspace-Id", out var headerVal) &&
                 Guid.TryParse(headerVal.FirstOrDefault(), out var parsedHeaderId))
        {
            workspaceId = parsedHeaderId;
        }

        if (workspaceId == Guid.Empty)
        {
            return;
        }

        // 4. Extract current user id
        var idString = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.User.FindFirstValue("sub")
                    ?? context.User.FindFirstValue("uid");

        if (!Guid.TryParse(idString, out var userId))
        {
            return;
        }

        // 5. Query workspace membership and role using scoped IUnitOfWork
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var member = await unitOfWork.WorkspaceMembers.FirstOrDefaultAsync(
            m => m.WorkspaceId == workspaceId && m.UserId == userId && m.IsActive && m.LeftAt == null);

        if (member == null)
        {
            return;
        }

        // 6. Check RBAC matrix according to AUTHORIZATION_MODEL.md:
        // - Students.Manage: Owner, Teacher, Assistant (Delegated or Non-Delegated)
        // - Students.View: Owner, Teacher, Assistant, Reception, Accountant
        // - Credentials.Manage: Owner, Teacher, Assistant, Reception
        var isAuthorized = requirement.Permission switch
        {
            Permissions.StudentsManage => member.Role is "Owner" or "Teacher" or "Assistant",
            Permissions.StudentsView => member.Role is "Owner" or "Teacher" or "Assistant" or "Reception" or "Accountant",
            Permissions.CredentialsManage => member.Role is "Owner" or "Teacher" or "Assistant" or "Reception",
            _ => false
        };

        if (isAuthorized)
        {
            context.Succeed(requirement);
        }
    }
}
