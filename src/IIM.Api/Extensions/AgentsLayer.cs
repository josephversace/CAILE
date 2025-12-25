using IIM.Api.Models;
using IIM.Api.Services;
using IIM.Application.Workspace;
using IIM.Infrastructure.AI.Intent;
using IIM.Infrastructure.Embeddings;
using IIM.Infrastructure.Services;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntime;
using UglyToad.PdfPig.Tokenization;

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
			services.AddSingleton<OnnxEmbeddingGenerator>(sp =>
			{

				var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

				using var scope = scopeFactory.CreateScope();
				var templates = scope.ServiceProvider.GetRequiredService<IModelConfigurationTemplateService>();
				var template = templates.GetDefaultTemplateAsync().GetAwaiter().GetResult();

				var cfg = template?.Models?.Embedding
					?? throw new InvalidOperationException("Embedding model not configured.");

				return new OnnxEmbeddingGenerator(cfg);
			});

			// ---- Interface mappings (NO new instances) ----
			services.AddSingleton<IEmbeddingGenerator<EmbeddingWorkItem, Embedding<float>>>(sp =>
				sp.GetRequiredService<OnnxEmbeddingGenerator>());

			services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
				sp.GetRequiredService<OnnxEmbeddingGenerator>());

			services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
				"embedding_model",
				(sp, _) => sp.GetRequiredService<OnnxEmbeddingGenerator>());

			// Other services
			services.AddSingleton<IMultimodalVisionService, MultimodalVisionService>();
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



			// Intent (control-plane model)
			// Single instance of the intent engine
			services.AddSingleton<IWorkspaceIntentEngine>(sp =>
			{
				var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

				// One-time scoped resolution just to read from EF
				using var scope = scopeFactory.CreateScope();
				var templates = scope.ServiceProvider.GetRequiredService<IModelConfigurationTemplateService>();
				var template = templates.GetDefaultTemplateAsync().GetAwaiter().GetResult();

				var modelPath = template.Models.Intent.LocalPath; // plain string, no EF dependency

				return new Phi3WorkspaceIntentEngine(modelPath);
			});



			// Policy (pure decision logic)
			services.AddScoped<IWorkspaceEvidencePlanner, WorkspaceEvidencePlanner>();

			// Context orchestration (per request)
			services.AddScoped<IWorkspaceContextManager, WorkspaceContextManager>();



			return services;
		}
	}

}