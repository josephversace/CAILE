using GraphRag.Config;
using GraphRag.Indexing;

public class GraphRagPipeline
{
	private readonly IndexingPipelineRunner _pipelineRunner;

	public GraphRagPipeline(IndexingPipelineRunner pipelineRunner)
	{
		_pipelineRunner = pipelineRunner;
	}

	public async Task IndexDocumentsAsync(CancellationToken ct)
	{
		var config = new GraphRagConfig
		{
			Input = new InputConfig
			{
				Storage = new StorageConfig { BaseDir = "input", Type = StorageType.File },
				FileType = InputFileType.Text,
				FilePattern = @".*\.txt$"
			},
			Output = new StorageConfig { BaseDir = "output", Type = StorageType.File },
			Chunks = new ChunkingConfig { Size = 1200, Overlap = 100 },
			ExtractGraph = new ExtractGraphConfig
			{
				ModelId = "chat_model",
				EntityTypes = ["person", "organization", "location", "event"]
			},
			ClusterGraph = new ClusterGraphConfig
			{
				Algorithm = CommunityDetectionAlgorithm.FastLabelPropagation,
				MaxClusterSize = 25
			}
		};

		var results = await _pipelineRunner.RunAsync(config, ct);

		foreach (var result in results)
		{
			Console.WriteLine($"Workflow: {result.Workflow}, Errors: {result.Errors?.Count ?? 0}");
		}
	}
}
