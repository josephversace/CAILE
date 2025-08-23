// ============================================
// File: src/IIM.Api/Extensions/AuditServiceExtensions.cs
// Wire up audit services properly
// ============================================

using IIM.Core.Configuration;
using IIM.Core.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IIM.Api.Extensions
{
    public static class AuditServiceExtensions
    {
        public static IServiceCollection AddAuditServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add HTTP context accessor for user tracking
            services.AddHttpContextAccessor();

            // Add user context service
            services.TryAddScoped<IUserContext, UserContextService>();

            // Add audit logger
            services.TryAddScoped<IAuditLogger, SqliteAuditLogger>();

            // Add audit configuration
            services.Configure<AuditConfiguration>(
                configuration.GetSection("Audit"));

            return services;
        }
    }
}