using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Config;
using IIM.Shared.Models.Configuration;

namespace IIM.Shared.Models
{
	
	public class CaileConfig
	{

		public string ApiBaseUrl { get; set; } = "http://localhost:5000";
		public DeploymentConfig Deployment { get; set; } = new();
		public SetupConfig Setup { get; set; } = new();
		public DatabaseConfig Database { get; set; } 
		public StorageConfig Storage { get; set; } = new();
		public GraphRagConfig GraphRag { get; set; } = new();
		public RedisConfig Redis { get; set; } = new();
		public SeaweedFsConfig SeaweedFS { get; set; } = new();
		public QdrantConfig Qdrant { get; set; } = new();
		public Neo4jConfig Neo4j { get; set; } = new();
		public DoclingConfig Docling { get; set; } = new();
		public SearXngConfig SearXNG { get; set; } = new();
		public PlaywrightConfig Playwright { get; set; } = new();
		public KreuzbergConfig Kreuzberg { get; set; } = new();
		public HangfireConfig Hangfire { get; set; } = new();
		public DataRouterConfig DataRouter { get; set; } = new();
		public ManagedFilesConfig ManagedFiles { get; set; } = new();
		public InferenceConfig Inference { get; set; } = new();
		public AuditConfig Audit { get; set; } = new();
		public JwtConfig Jwt { get; set; } = new();
		public LoggingConfig Logging { get; set; } = new();
		public ModelsConfig Models { get; set; } = new();

		public ToolsConfig Tools { get; set; } = new();

		public EnrichmentConfig Enrichment { get; set; } = new();



	}

}
