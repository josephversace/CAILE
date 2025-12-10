using IIM.Infrastructure.Models;
using IIM.Shared.Configuration;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Options;

public static class ConfigurationLayer
{
	public static IServiceCollection AddBoundConfiguration(
		this IServiceCollection services,
		IConfiguration config)
	{
		// ------------------------------------------------------------
		// 1. Bind the ENTIRE appsettings.json into CaileConfig
		// ------------------------------------------------------------
		services.Configure<CaileConfig>(config);

		// ------------------------------------------------------------
		// 2. Register CaileConfig as a concrete singleton for DI
		//    (Services inject CaileConfig directly — no IOptions needed)
		// ------------------------------------------------------------
		services.AddSingleton(sp =>
			sp.GetRequiredService<IOptions<CaileConfig>>().Value);



		return services;
	}
}
