using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MyApp.API.Filters;

/// <summary>
/// Restricts Hangfire dashboard access to authenticated users with Admin or SuperAdmin roles.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        return httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("SuperAdmin");
    }
}
