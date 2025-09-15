using IIM.Api.Services;
using IIM.Application.Behaviors;
using IIM.Application.Behaviours;
using IIM.Core.Mediator;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IIM.Api.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services)
		{
			// Register Mediator and its behaviors from the Application assembly
			services.AddSimpleMediator(typeof(IManagedFileManager).Assembly);
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			// Add other high-level application services here if needed...

			return services;
		}

		public static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Background service for monitoring evidence integrity
			services.AddHostedService<EvidenceIntegrityMonitor>();

			// Add other background services here...

			return services;
		}
	}
}

