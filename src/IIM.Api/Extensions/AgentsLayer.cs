using IIM.Api.Services;
using IIM.Application.Urls;
using IIM.Infrastructure.AI.Intent;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

			// ---- SINGLE embedding generator instance ----


			services.AddSingleton<OllamaEmbeddingGenerator>(sp =>
			{
				var config = sp.GetRequiredService<IOptions<CaileConfig>>().Value;

				var embeddingConfig = config.Models.Infrastructure.Embedding;
				var providerConfig = config.Models.Provider;

				return new OllamaEmbeddingGenerator(embeddingConfig, providerConfig);
			});

			services.AddSingleton<IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>>(sp =>
				sp.GetRequiredService<OllamaEmbeddingGenerator>());

			services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
				sp.GetRequiredService<OllamaEmbeddingGenerator>());

			services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
				"embedding_model",
				(sp, _) => sp.GetRequiredService<OllamaEmbeddingGenerator>());
			// Update interface mappings
			services.AddSingleton<IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>>(sp =>
				sp.GetRequiredService<OllamaEmbeddingGenerator>());

			services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
				sp.GetRequiredService<OllamaEmbeddingGenerator>());

			services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
				"embedding_model",
				(sp, _) => sp.GetRequiredService<OllamaEmbeddingGenerator>());

			// Other services

			services.AddSingleton<IEmbeddingService, EmbeddingService>();

			services.AddSingleton<IChatClient>(sp =>
			{
				var factory = sp.GetRequiredService<IAIAgentFactory>();

				// Build once, synchronously, fail fast if misconfigured
				return factory.GetChatClientAsync()
					.GetAwaiter()
					.GetResult();
			});

			services.AddKeyedSingleton<IChatClient>(
				"chat_model",
				(sp, _) => sp.GetRequiredService<IChatClient>());

			// Intent engine using Ollama
			services.AddSingleton<IWorkspaceIntentEngine>(sp =>
			{
				var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

				using var scope = scopeFactory.CreateScope();
				var configService = scope.ServiceProvider.GetRequiredService<IModelConfigurationService>();
				var config = configService.GetConfigurationAsync().GetAwaiter().GetResult();

				var endpoint = config.Provider.Endpoint;

				// Use the primary model for intent classification
				// It's lightweight enough and avoids loading another model
				var modelId = config.Active.Primary.ModelId;

				return new OllamaWorkspaceIntentEngine(endpoint, modelId);
			});
			// Policy (pure decision logic)
			services.AddScoped<IWorkspaceEvidencePlanner, WorkspaceEvidencePlanner>();

			// Context orchestration (per request)
			services.AddScoped<IWorkspaceContextManager, WorkspaceContextManager>();

			//Tool Routering Service
			services.AddScoped<IToolRoutingService, ToolRoutingService>();

			services.AddHttpClient<IPlaywrightService, PlaywrightService>();
			services.AddHttpClient<ISearchService, SearXngService>();
			services.AddScoped<WebTools>();

			return services;
		}
	}
}