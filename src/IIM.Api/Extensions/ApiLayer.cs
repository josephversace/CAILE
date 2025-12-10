using IIM.Shared.Configuration;

namespace IIM.Api.Extensions
{
	public static class ApiLayer
	{
		public static IServiceCollection AddApiLayer(
			this IServiceCollection services,
			IConfiguration config,
			DeploymentConfiguration deployment)
		{
			services.AddSignalR(o =>
			{
				o.EnableDetailedErrors = deployment.IsDevelopment;
			});

			services.AddCors(o => o.AddPolicy("_caileCors", p =>
			{
				p.WithOrigins("http://localhost:5056")
				 .AllowAnyHeader()
				 .AllowAnyMethod()
				 .AllowCredentials();
			}));

			return services;
		}
	}

}
