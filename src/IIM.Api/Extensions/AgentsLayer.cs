using IIM.Api.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.AI;

namespace IIM.Api.Extensions
{

	public static class AgentsLayer
	{
		public static IServiceCollection AddAgentsLayer(this IServiceCollection services)
		{
			// Tool registry
			services.AddSingleton<IToolRegistry, ToolRegistry>();

			// Agent factory
			services.AddSingleton<IAIAgentFactory, AIAgentFactory>();

			return services;
		}
	}
}