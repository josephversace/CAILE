using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Data
{
    /// <summary>
    /// Background service to ensure database is created and migrated on startup
    /// </summary>
    public class DatabaseMigrationService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseMigrationService> _logger;

        public DatabaseMigrationService(
            IServiceProvider serviceProvider,
            ILogger<DatabaseMigrationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Ensuring database is created and up to date");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IIMDbContext>();

            try
            {
                var created = await context.Database.EnsureCreatedAsync(cancellationToken);

                if (created)
                {
                    // Don't use GetConnectionString() - it doesn't exist
                    _logger.LogInformation("Database created successfully");
                }

                var modelCount = await context.ModelMetadata.CountAsync(cancellationToken);
                var auditCount = await context.AuditLogs.CountAsync(cancellationToken);
                // Remove InvestigationTemplates count

                _logger.LogInformation("Database ready: {Models} models, {Audits} audit entries",
                    modelCount, auditCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}