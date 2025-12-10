using Hangfire;
using IIM.Api.Services;

namespace IIM.Api.Extensions
{


	public static class HostedWorkersExtensions
	{
		public static IServiceCollection AddHostedWorkers(
			this IServiceCollection services,
			IConfiguration config)
		{

			services.AddHostedService<FileIntegrityMonitor>();

	

			// Add Hangfire server (this starts worker threads)
			services.AddHangfireServer(options =>
			{
				options.WorkerCount = config.GetValue<int>("Hangfire:WorkerCount", 2);
			});

			return services;
		}
	}


}
