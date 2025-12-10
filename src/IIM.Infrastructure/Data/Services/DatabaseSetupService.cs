using Hangfire;
using Hangfire.PostgreSql;
using IIM.Infrastructure.Data;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace IIM.Infrastructure.Data
{
	public static class DatabaseLayer
	{
		public static IServiceCollection AddIIMDatabases(
			this IServiceCollection services,
			IConfiguration config)
		{
			var tier = config.GetValue<string>("Deployment:Tier", "mini")?.ToLowerInvariant();
			var isDev = config.GetValue<bool>("Deployment:IsDevelopment", false);

			return tier switch
			{
				"micro" or "mini" or "small"
					=> services.AddPostgresDatabases(config, isDev),

				_ => throw new InvalidOperationException(
					$"Invalid deployment tier '{tier}'. Expected: micro, mini, small.")
			};
		}

		// ---------------------------------------------------------------------
		// POSTGRES IMPLEMENTATION
		// ---------------------------------------------------------------------
		private static IServiceCollection AddPostgresDatabases(
			this IServiceCollection services,
			IConfiguration config,
			bool isDevelopment)
		{
			var host = config["Database:Host"] ?? "localhost";
			var port = config.GetValue<int>("Database:Port", 5432);
			var username = config["Database:Username"] ?? "iim_user";
			var password = config["Database:Password"]
				?? throw new InvalidOperationException("Database:Password is missing.");

			string Conn(string db) =>
				$"Host={host};Port={port};Database={db};Username={username};Password={password}";

			// -----------------------------------------------------------------
			// 1. Register DbContexts
			// -----------------------------------------------------------------
	
			services.AddDbContext<ConfigDbContext>(opt => opt.UseNpgsql(Conn("iim_config")));
			services.AddDbContext<AuthDbContext>(opt =>
			{
				opt.UseNpgsql(Conn("iim_identity"));
				opt.UseOpenIddict();
			});
			services.AddDbContext<AuditDbContext>(opt => opt.UseNpgsql(Conn("iim_audit")));
			services.AddDbContext<GovernanceDbContext>(opt => opt.UseNpgsql(Conn("iim_governance")));
			services.AddDbContext<WorkspaceDbContext>(opt => opt.UseNpgsql(Conn("iim_workspace")));

			// -----------------------------------------------------------------
			// 2. Hangfire Database
			// -----------------------------------------------------------------
			var jobsDb = "iim_jobs";
			EnsureDatabaseExists(host, port, username, password, jobsDb);

			services.AddHangfire(config => config
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(Conn(jobsDb)))
			);

			// -----------------------------------------------------------------
			// 3. Repositories
			// -----------------------------------------------------------------
			services.AddScoped<IConfigRepository, EfConfigRepository>();
			services.AddScoped<IAuditRepository, EfAuditRepository>();
			services.AddScoped<IGovernanceRepository, EfGovernanceRepository>();
			services.AddScoped<IRoleRepository, EfRoleRepository>();
			services.AddScoped<IWorkspaceManager, EfWorkspaceManager>();
			services.AddScoped<IUserRepository, EfUserRepository>();

			// -----------------------------------------------------------------
			// 4. Migration Runner
			// -----------------------------------------------------------------
			services.AddTransient<DatabaseMigrationRunner>();

			return services;
		}

		// ---------------------------------------------------------------------
		// DATABASE CREATION HELPER
		// ---------------------------------------------------------------------
		private static void EnsureDatabaseExists(
			string host,
			int port,
			string username,
			string password,
			string database)
		{
			var master = $"Host={host};Port={port};Database=postgres;Username={username};Password={password}";

			using var conn = new Npgsql.NpgsqlConnection(master);
			conn.Open();

			// Check
			using (var cmd = conn.CreateCommand())
			{
				cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database}'";
				var exists = cmd.ExecuteScalar() != null;
				if (exists) return;
			}

			// Create db
			using (var cmd = conn.CreateCommand())
			{
				cmd.CommandText = $"CREATE DATABASE \"{database}\"";
				cmd.ExecuteNonQuery();
			}
		}
	}
}
