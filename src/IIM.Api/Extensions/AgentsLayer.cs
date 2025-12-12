using IIM.Api.Models;
using IIM.Api.Services;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

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



			services.AddSingleton<IMultimodalVisionService, MultimodalVisionService>();

			services.AddSingleton<IEmbeddingService, EmbeddingService>();

			services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
			{
				var templates = sp.GetRequiredService<IModelConfigurationTemplateService>();
				var template = templates.GetDefaultTemplateAsync().GetAwaiter().GetResult();

				var cfg = template?.Models?.Embedding
					?? throw new InvalidOperationException("Embedding model not configured.");

				return new OnnxEmbeddingGenerator(cfg);
			});


			return services;
		}
	}
}