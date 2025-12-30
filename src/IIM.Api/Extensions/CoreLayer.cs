using IIM.Core.Services;
using IIM.Core.Services.Gpu;

using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;

namespace IIM.Api.Extensions
{
	public static class CoreLayer
	{
		public static IServiceCollection AddCoreLayer(
			this IServiceCollection services,
			IConfiguration config,
			DeploymentConfiguration deployment)
		{
			services.AddSingleton<IGpuProbeService, GpuProbeService>();

		

			services.AddScoped<IUserContext, UserContextService>();

			return services;
		}
	}

}
