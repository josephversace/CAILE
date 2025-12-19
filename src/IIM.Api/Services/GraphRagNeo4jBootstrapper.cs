namespace IIM.Api.Services
{
	using GraphRag.Graphs;

	public sealed class GraphRagNeo4jBootstrapper : IHostedService
	{
		private readonly IGraphStore _graph;
		private readonly ILogger<GraphRagNeo4jBootstrapper> _logger;

		public GraphRagNeo4jBootstrapper(
			IGraphStore graph,
			ILogger<GraphRagNeo4jBootstrapper> logger)
		{
			_graph = graph;
			_logger = logger;
		}

		public async Task StartAsync(CancellationToken ct)
		{
			_logger.LogInformation("Initializing GraphRAG Neo4j graph store...");
			await _graph.InitializeAsync(ct);
			_logger.LogInformation("GraphRAG Neo4j graph store initialized.");
		}

		public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
	}

}
