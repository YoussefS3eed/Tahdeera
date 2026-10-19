using Microsoft.AspNetCore.Authorization;

namespace Tahdeera.API.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public HasPermissionAttribute(string permission) : base(policy: permission)
    {
        Permission = permission;
    }
}
