using IIM.Core.Services;
using IIM.Core.Services.Gpu;
using IIM.Infrastructure.AI.DirectML;
using IIM.Infrastructure.AI.Execution;
using IIM.Infrastructure.AI.OnnxRuntime;
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

			services.AddSingleton<IOnnxExecutionProvider>(sp =>
			{
				var gpu = sp.GetRequiredService<IGpuProbeService>();

				if (gpu.HasCuda)
					return new CudaExecutionProvider();

				if (gpu.HasDirectML)
					return new DirectMLExecutionProvider(
						sp.GetRequiredService<IDirectMLDeviceManager>()
					);

				if (gpu.HasMetal)
					return new MetalExecutionProvider();

				return new CpuExecutionProvider();
			});


			services.AddScoped<IUserContext, UserContextService>();

			return services;
		}
	}

}
