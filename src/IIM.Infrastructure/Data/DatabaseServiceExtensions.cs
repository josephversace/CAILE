using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IIM.Infrastructure.Data.Services;
using IIM.Infrastructure.Storage;

namespace IIM.Infrastructure.Data
{
    /// <summary>
    /// Extension methods for registering database services
    /// </summary>
    public static class DatabaseServiceExtensions
    {
        /// <summary>
        /// Adds SQLite database support with Entity Framework Core
        /// </summary>
        public static IServiceCollection AddIIMDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Get storage configuration
            var storageConfig = configuration.Get<StorageConfiguration>() ?? new StorageConfiguration();

            // Register DbContext with SQLite
            services.AddDbContext<IIMDbContext>(options =>
            {
                var connectionString = $"Data Source={storageConfig.SqlitePath}";
                options.UseSqlite(connectionString);

                // Enable sensitive data logging in development
#if DEBUG
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
#endif
            });

            // Register database-backed services
            services.AddScoped<IModelMetadataService, DatabaseModelMetadataService>();

            // Add migration service to ensure database is created
            services.AddHostedService<DatabaseMigrationService>();

            return services;
        }
    }
}
