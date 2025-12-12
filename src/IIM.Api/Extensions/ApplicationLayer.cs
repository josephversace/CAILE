using IIM.Application.Behaviors;
using IIM.Application.Files;
using IIM.Shared.Mediator;

namespace IIM.Api.Extensions
{
	public static class ApplicationLayer
	{
		public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
		{
			services.AddSimpleMediator(typeof(RegisterUploadedFileCommand).Assembly);

			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			return services;
		}
	}

}
