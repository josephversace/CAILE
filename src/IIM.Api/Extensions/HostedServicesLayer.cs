using Hangfire;
using IIM.Api.Services;
using IIM.Application.Files;

namespace IIM.Api.Extensions
{


	public static class HostedWorkersExtensions
	{
		public static IServiceCollection AddHostedWorkers(
			this IServiceCollection services,
			IConfiguration config)
		{

			services.AddHostedService<FileIntegrityMonitor>();

			services.AddHostedService<GraphRagNeo4jBootstrapper>();

			// Add Hangfire server (this starts worker threads)
			services.AddHangfireServer(options =>
			{
				options.WorkerCount = config.GetValue<int>("Hangfire:WorkerCount", 2);
			});

			// In your DI setup (if not auto-discovered)
			services.AddScoped<IngestionJob>();

			return services;
		}
	}


}
