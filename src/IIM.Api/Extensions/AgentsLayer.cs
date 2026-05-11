using IIM.Api.Services;
using IIM.Application.Urls;
using IIM.Infrastructure.AI.Intent;
using IIM.Infrastructure.Data;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Configuration;
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

				var embeddingConfig = config.Models.Infrastructure.Models["embedding"];
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

				var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();

				// Resolve the intent-capable model
				var model = resolver
					.GetIntentModelAsync()
					.GetAwaiter()
					.GetResult();

				var provider = resolver
					.GetProviderAsync(model)
					.GetAwaiter()
					.GetResult();

				return new OllamaWorkspaceIntentEngine(
					provider.Endpoint!,
					model.ModelId
				);
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

		

			services.AddScoped<IPromptStore, EfPromptStore>();
			services.AddScoped<IPromptSnapshotProvider, PromptSnapshotProvider>();
			services.AddScoped<PromptResolver>();


			return services;
		}
	}
}