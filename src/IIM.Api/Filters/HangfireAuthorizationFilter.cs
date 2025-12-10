// IIM.Api/Filters/HangfireAuthorizationFilter.cs
using Hangfire.Dashboard;
using IIM.Shared.Configuration;

namespace IIM.Api.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // In development, allow all
            if (httpContext.RequestServices
                .GetRequiredService<DeploymentConfiguration>()
                .IsDevelopment)
            {
                return true;
            }

            // In production, require authentication
            return httpContext.User.Identity?.IsAuthenticated ?? false;
        }
    }
}