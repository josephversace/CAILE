
using IIM.Infrastructure.Storage;
using IIM.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Core.Configuration
{
    /// <summary>
    /// Extension methods for registering database services
    /// </summary>
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Adds SQLite database support with Entity Framework Core
        /// </summary>
        public static IServiceCollection AddIIMDatabases(
            this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddDbContext<AuditDbContext>(options =>
                   options.UseSqlite(configuration.GetConnectionString("AuditDb")));

            services.AddDbContext<ConfigDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("ConfigDb")));


            services.AddDbContext<ModelDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("ModelDb")));


            return services;
        }
    }
}
