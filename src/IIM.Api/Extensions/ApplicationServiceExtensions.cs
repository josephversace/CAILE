using IIM.Api.Services;
using IIM.Application.Behaviors;
using IIM.Application.Files;
using IIM.Application.ManagedFiles;
using IIM.Shared.Interfaces;
using IIM.Shared.Mediator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace IIM.Api.Extensions
{
	public static class ApplicationServiceExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services)
		{
			// Register Mediator and its behaviors from the Application assembly

            services.AddSimpleMediator(
        typeof(ProcessUploadedFileCommandHandler).Assembly,  // Application assembly
        typeof(RequestUploadUrlCommandHandler).Assembly,     // Same assembly
        typeof(IManagedFileManager).Assembly
    );
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped<IRequestHandler<ProcessUploadedFileCommand, ProcessUploadedFileResult>, ProcessUploadedFileCommandHandler>();
            services.AddScoped<IRequestHandler<RequestUploadUrlCommand, RequestUploadUrlResult>, RequestUploadUrlCommandHandler>();


            return services;
		}

		public static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Background service for monitoring evidence integrity
			services.AddHostedService<FileIntegrityMonitor>();

			// Add other background services here...

			return services;
		}
	}
}

