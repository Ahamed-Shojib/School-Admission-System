using Microsoft.AspNetCore.Mvc.Filters;
using SchoolAdmission.Api.Auth;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Filters;

/// <summary>Requires "Authorization: Bearer &lt;admin token&gt;" (skipped when ADMIN_AUTH_REQUIRED is false).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireAdminAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var services = context.HttpContext.RequestServices;
        if (!services.GetRequiredService<AdminAuthOptions>().Required)
            return;

        var header = context.HttpContext.Request.Headers.Authorization.ToString();
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;

        if (!services.GetRequiredService<AdminTokenService>().IsValid(token))
            throw new UnauthorizedException("ADMIN_AUTH_REQUIRED", "Admin login is required for this action.");
    }
}
