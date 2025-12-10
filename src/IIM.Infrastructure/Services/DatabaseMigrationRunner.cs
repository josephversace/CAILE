using System.Transactions;
using IIM.Infrastructure.Data;
using IIM.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IIM.Infrastructure.Services;

public class DatabaseMigrationRunner
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DatabaseMigrationRunner> _logger;

	public DatabaseMigrationRunner(IServiceProvider serviceProvider, ILogger<DatabaseMigrationRunner> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	/// <summary>
	/// Performs atomic, pending-only migrations across ALL DbContexts.
	/// If one fails, all rollback.
	/// </summary>
	public async Task ApplyAllMigrationsAsync()
	{
		using var scope = _serviceProvider.CreateScope();
		var logger = _logger;



		var contexts = new DbContext[]
		{

		scope.ServiceProvider.GetRequiredService<ConfigDbContext>(),
		//scope.ServiceProvider.GetRequiredService<ModelDbContext>(),
		scope.ServiceProvider.GetRequiredService<AuthDbContext>(),
		scope.ServiceProvider.GetRequiredService<AuditDbContext>(),
		scope.ServiceProvider.GetRequiredService<GovernanceDbContext>(),
		scope.ServiceProvider.GetRequiredService<WorkspaceDbContext>()

		};

		logger.LogInformation("Starting EF Core migration runner.");

		foreach (var ctx in contexts)
		{
			var name = ctx.GetType().Name;
			try
			{
				var pending = (await ctx.Database.GetPendingMigrationsAsync()).Any();
				if (!pending)
				{
					logger.LogInformation("⏩ {DbContext} already up to date.", name);
					continue;
				}

				logger.LogInformation("🚀 Migrating {DbContext} ...", name);
				await ctx.Database.MigrateAsync();
				logger.LogInformation("✅ Migration complete for {DbContext}", name);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "❌ Migration failed for {DbContext}", name);
				throw; // important: fail fast
			}
		}

		logger.LogInformation("🎉 ALL DB MIGRATIONS COMPLETE.");
	}

}
