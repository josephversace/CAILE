using System;
using System.Threading;
using System.Threading.Tasks;
using Blake3;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class IngestionRunnerService : IIngestionRunner
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IngestionStepRunner _stepRunner;
	private readonly ILogger<IngestionRunnerService> _logger;

	// pipeline services used by steps
	private readonly IExifToolService _exifTool;
	private readonly DocumentExtractionRouter _documentRouter;
	private readonly DocumentShapeDetector _shapeDetector;
	private readonly ChunkingStrategyFactory _chunkingFactory;
	private readonly IEmbeddingService _embedding;
	private readonly IQdrantService _qdrant;
	private readonly IndicatorExtractor _indicatorExtractor;
	private readonly IAIAgentFactory _agentFactory;
	private readonly ExcelStructureDetector _excelDetector;
	private readonly ExcelCanonicalizer _excelCanonicalizer;

	public IngestionRunnerService(
		IWorkspaceManager workspace,
		IFileStore files,
		IngestionStepRunner stepRunner,
		IExifToolService exifTool,
		DocumentExtractionRouter documentRouter,
		DocumentShapeDetector shapeDetector,
		ChunkingStrategyFactory chunkingFactory,
		IEmbeddingService embedding,
		IQdrantService qdrant,
		IndicatorExtractor indicatorExtractor,
		IAIAgentFactory agentFactory,
		ExcelStructureDetector excelDetector,
		ExcelCanonicalizer excelCanonicalizer,
		ILogger<IngestionRunnerService> logger)
	{
		_workspace = workspace;
		_files = files;
		_stepRunner = stepRunner;
		_exifTool = exifTool;
		_documentRouter = documentRouter;
		_shapeDetector = shapeDetector;
		_chunkingFactory = chunkingFactory;
		_embedding = embedding;
		_qdrant = qdrant;
		_indicatorExtractor = indicatorExtractor;
		_agentFactory = agentFactory;
		_excelDetector = excelDetector;
		_excelCanonicalizer = excelCanonicalizer;
		_logger = logger;
	}

	public async Task<IngestionResult> RunAsync(Guid virtualFileId, IngestionRunOptions? options, CancellationToken ct)
	{
		options ??= IngestionRunOptions.Default;

		var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct)
			?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");

		var stored = vf.StoredFile
			?? throw new InvalidOperationException("StoredFile missing.");

		using var hasher = new Blake3HashAlgorithm();
		using var stopCts = new CancellationTokenSource();
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, stopCts.Token);

		// ✅ cache bytes so multiple steps don't re-read from store
		byte[]? bytesCache = null;

		Task<byte[]> ReadBytesAsync(CancellationToken token)
			=> bytesCache is not null
				? Task.FromResult(bytesCache)
				: LoadAsync(token);

		async Task<byte[]> LoadAsync(CancellationToken token)
		{
			var b = await _files.ReadAsync(stored.Bucket, stored.Blake3Hash, token).ConfigureAwait(false);
			bytesCache = b;
			return b;
		}

		var stepCtx = new IngestionStepContext
		{
			VirtualFile = vf,
			StoredFile = stored,
			Hasher = hasher,
			Workspace = _workspace,
			Files = _files,
			Logger = _logger,

			ExifTool = _exifTool,
			DocumentRouter = _documentRouter,
			ShapeDetector = _shapeDetector,
			ChunkingFactory = _chunkingFactory,
			Embedding = _embedding,
			Qdrant = _qdrant,
			IndicatorExtractor = _indicatorExtractor,
			AgentFactory = _agentFactory,
			ExcelDetector = _excelDetector,
			ExcelCanonicalizer = _excelCanonicalizer,
			CurrentStepId = "Initial", // Will be updated by the StepRunner during execution
			Overrides = options.Overrides ?? new Dictionary<string, string>(),

			StopCts = stopCts,

		
			ReadBytesAsync = ReadBytesAsync
		};

		await _stepRunner.RunAsync(stepCtx, options, linked.Token).ConfigureAwait(false);


		var deduplicated = stepCtx.Bag.TryGetValue("deduplicated", out var dd) && dd is bool b && b;
		var chunkCount = stepCtx.Bag.TryGetValue("chunk_count", out var cc) && cc is int ci ? ci : 0;
		var vectorCount = stepCtx.Bag.TryGetValue("vector_count", out var vc) && vc is int vi ? vi : 0;
		var entityCount = stepCtx.Bag.TryGetValue("entity_count", out var ec) && ec is int ei ? ei : 0;

		return new IngestionResult
		{
			StoredId = stored.Blake3Hash,
			Deduplicated = deduplicated,
			ChunkCount = chunkCount,
			VectorCount = vectorCount,
			EntityCount = entityCount,
			CompletedAt = DateTime.UtcNow
		};
	}
}
