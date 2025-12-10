using GraphRag;
using GraphRag.Storage.Neo4j;
using IIM.Application.Services;
using IIM.Infrastructure.Docling;
using IIM.Infrastructure.Foundry;
using IIM.Infrastructure.Models;
using IIM.Infrastructure.Services;
using IIM.Infrastructure.Storage;
using IIM.Infrastructure.Templates;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;

namespace IIM.Api.Extensions
{
	public static class InfrastructureLayer
	{
		public static IServiceCollection AddInfrastructureLayer(
			this IServiceCollection services,
			IConfiguration config)
		{
			// STORAGE
			services.AddSingleton<IObjectStorageProvider, SeaweedFSStorageProvider>();

			// FILE INTEGRITY / HASHING
			services.AddScoped<IHashService, HashService>();
			services.AddScoped<IFileIntegrityService, FileIntegrityService>();
			services.AddScoped<IAuditService, AuditService>();

		

			//SewaeedFS
			services.AddHttpClient<IFileStore, SeaweedFileStore>((sp, client) =>
			{
				var cfg = sp.GetRequiredService<CaileConfig>().SeaweedFS;
				client.BaseAddress = new Uri(cfg.FilerUrl);
			});


			// FOUNDRY
			services.AddSingleton<IFoundryEndpointProvider, FoundryEndpointProvider>();
			services.AddSingleton<IFoundryStatusChecker, FoundryStatusChecker>();
			services.AddSingleton<IFoundryModelService, FoundryModelService>();
			services.AddHostedService<FoundryStartupService>();

			//Docling
			services.AddHttpClient<IDoclingService, DoclingService>((sp, client) =>
			{
				var cfg = sp.GetRequiredService<CaileConfig>().Docling;
				client.BaseAddress = new Uri(cfg.BaseUrl);
				if (cfg.TimeoutSeconds > 0)
					client.Timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
			});


			// GRAPH RAG + NEO4J
			services.AddGraphRag();
			services.AddNeo4jGraphStore("neo4j", opts =>
			{
				var s = config.GetSection("GraphRag:GraphStores:neo4j");
				opts.Uri = s["Uri"]!;
				opts.Username = s["Username"]!;
				opts.Password = s["Password"]!;
			});

			services.AddScoped<IModelConfigurationTemplateService, ModelTemplateService>();
			services.AddScoped<IModelTemplateResolver, ModelTemplateResolver>();


			return services;
		}
	}

}
