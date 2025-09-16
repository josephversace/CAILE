using IIM.Infrastructure.Data;
using IIM.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Infrastructure.Data
{
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Adds database support with Entity Framework Core
        /// Supports both PostgreSQL (production) and SQLite (development)
        /// </summary>
        public static IServiceCollection AddIIMDatabases(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var useSqliteForDev = configuration.GetValue<bool>("Development:UseSqliteForDev", false);
            var isDevelopment = configuration.GetValue<bool>("Deployment:IsDevelopment", false);

            if (isDevelopment && useSqliteForDev)
            {
                // SQLite for development
                services.AddDbContext<FileDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

                services.AddDbContext<GovernanceDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("ConfigDb")));

                services.AddDbContext<AuditDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("AuditDb")));

                services.AddDbContext<ConfigDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("ConfigDb")));

                services.AddDbContext<ModelDbContext>(options =>
                    options.UseSqlite(configuration.GetConnectionString("ModelDb")));
            }
            else
            {
                // PostgreSQL for production
                services.AddDbContext<FileDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

                services.AddDbContext<GovernanceDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("ConfigDb")));

                services.AddDbContext<AuditDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("AuditDb")));

                services.AddDbContext<ConfigDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("ConfigDb")));

                services.AddDbContext<ModelDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("ModelDb")));
            }

            // Register repositories
            services.AddScoped<IAuditRepository, EfAuditRepository>();
            services.AddScoped<IConfigRepository, EfConfigRepository>();
            services.AddScoped<IModelRepository, EfModelRepository>();
            services.AddScoped<IModelParameterSetRepository, EfModelParameterSetRepository>();
            services.AddScoped<IGovernanceRepository, EfGovernanceRepository>();
            services.AddScoped<IWorkspaceProvider, PostgresWorkspaceProvider>();

            return services;
        }
    }
}