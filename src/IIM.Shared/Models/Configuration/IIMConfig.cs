using System;
using System.Collections.Generic;
using System.Text;
using GraphRag.Config;

namespace IIM.Shared.Models
{
	
	public class CaileConfig
	{
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
		public HangfireConfig Hangfire { get; set; } = new();
		public DataRouterConfig DataRouter { get; set; } = new();
		public ManagedFilesConfig ManagedFiles { get; set; } = new();
		public InferenceConfig Inference { get; set; } = new();
		public AuditConfig Audit { get; set; } = new();
		public JwtConfig Jwt { get; set; } = new();
		public LoggingConfig Logging { get; set; } = new();
		public ModelTemplatesConfig ModelTemplates { get; set; } = new();

		public EnrichmentConfig Enrichment { get; set; } = new();

	}

}
