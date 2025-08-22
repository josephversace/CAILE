using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Configuration
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
            var auditcontext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
            var configcontext = scope.ServiceProvider.GetRequiredService<ConfigDbContext>();
            var modelcontext = scope.ServiceProvider.GetRequiredService<ModelDbContext>();

            try
            {
                var created = await auditcontext.Database.EnsureCreatedAsync(cancellationToken);

                if (created)
                {
                    // Don't use GetConnectionString() - it doesn't exist
                    _logger.LogInformation("Database created successfully");
                }

                var modelCount = await modelcontext.ModelMetadata.CountAsync(cancellationToken);
                var auditCount = await auditcontext.AuditLogs.CountAsync(cancellationToken);
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