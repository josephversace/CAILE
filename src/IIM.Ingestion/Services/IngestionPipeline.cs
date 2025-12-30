using Blake3;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Ingestion.Services;

public sealed class IngestionPipeline : IIngestionPipeline
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IAIAgentFactory _agentFactory;
	private readonly IDoclingService _docling;
	private readonly DocumentShapeDetector _documentShapeDetector;
	private readonly ChunkingStrategyFactory _chunkingFactory;
	private readonly IGraphRagPipeline? _graphRag;
	private readonly GraphExtractionJob _graphExtractionJob;
	private readonly IEmbeddingService _embedding;
	private readonly IQdrantService _qdrant;
	private readonly DocumentExtractionRouter _documentRouter;
	private readonly CaileConfig _caileConfig;
	private readonly EntityLinkingJob? _entityLinking;
	private readonly IndicatorExtractor _indicatorExtractor;
	private readonly ILogger<IngestionPipeline> _logger;
	private const string PipelineVersion = "2.0";

	public IngestionPipeline(
	  IWorkspaceManager workspace,
	  IFileStore files,
	  IAIAgentFactory agentFactory,
	  IDoclingService docling,
	  DocumentShapeDetector documentShapeDetector,
	  ChunkingStrategyFactory chunkingFactory,
	  IGraphRagPipeline? graphRag,
	  GraphExtractionJob graphExtractionJob,
	  IEmbeddingService embedding,
	  DocumentExtractionRouter documentRouter,
	  IQdrantService qdrant,
	  CaileConfig caileConfig,
	  ILogger<IngestionPipeline> logger,
	  IndicatorExtractor indicatorExtractor,
	  EntityLinkingJob? entityLinking = null)
	{
		this._workspace = workspace;
		this._files = files;
		this._agentFactory = agentFactory;
		this._docling = docling;
		this._documentShapeDetector = documentShapeDetector;
		this._chunkingFactory = chunkingFactory;
		this._graphRag = graphRag;
		this._graphExtractionJob = graphExtractionJob;
		this._embedding = embedding;
		this._qdrant = qdrant;
		this._caileConfig = caileConfig;
		this._documentRouter = documentRouter;
		this._indicatorExtractor = indicatorExtractor;
		this._logger = logger;
		this._entityLinking = entityLinking;
	}

	public async Task<IngestionResult> IngestAsync(Guid virtualFileId, CancellationToken ct)
	{
		VirtualFile vf;
		StoredFile stored;
		string blake3Hash;
		byte[] bytes;
		string extractedText;
		DocumentShapeResult shapeResult;
		string extractedTextHash;
		ChunkingResult chunkingResult;
		VectorIndexResult vectorResult;
		using (Blake3HashAlgorithm hasher = new Blake3HashAlgorithm())
		{
			vf = await this._workspace.GetVirtualFileByIdAsync(virtualFileId, ct) ?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");
			stored = vf.StoredFile ?? throw new InvalidOperationException("StoredFile missing.");
			blake3Hash = stored.Blake3Hash;
			this._logger.LogInformation("Ingesting {FileName} [{Hash}] (Pipeline v{Version})", (object)vf.FileName, (object)blake3Hash.Substring(0, 12), (object)"2.0");
			if (await this._qdrant.ExistsAsync(blake3Hash, ct))
			{
				this._logger.LogInformation("Hash {Hash} already embedded. Attaching to workspace.", (object)blake3Hash.Substring(0, 12));
				await this._qdrant.AttachFileToExistingChunksAsync(blake3Hash, vf.WorkspaceId, vf.Id, ct);
				return new IngestionResult()
				{
					StoredId = blake3Hash,
					Deduplicated = true,
					CompletedAt = DateTime.UtcNow
				};
			}
			bytes = await this._files.ReadAsync(stored.Bucket, stored.StoragePath, ct);
			if (stored.MimeType.StartsWith("image/"))
			{
				IChatClient chatClientAsync = await this._agentFactory.GetChatClientAsync();
				string modelName = this._agentFactory.CurrentChatModel;
				List<ChatMessage> messages = new List<ChatMessage>(2)
		{
		  new ChatMessage(ChatRole.User, "Describe this image in detail."),
		  new ChatMessage(ChatRole.User, (IList<AIContent>) new List<AIContent>(1)
		  {
			(AIContent) new DataContent((ReadOnlyMemory<byte>) bytes, stored.MimeType)
		  })
		};
				CancellationToken cancellationToken = new CancellationToken();
				ChatResponse responseAsync = await chatClientAsync.GetResponseAsync((IEnumerable<ChatMessage>)messages, cancellationToken: cancellationToken);
				if (responseAsync != null)
				{
					string text = responseAsync.Text;
					string lowerInvariant = Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
					IWorkspaceManager workspace = this._workspace;
					ProcessedFile pf = new ProcessedFile();
					pf.StoredFileHash = stored.Blake3Hash;
					pf.DerivedHash = lowerInvariant;
					pf.ProcessorName = "ImageDescription";
					pf.ProcessorKind = modelName;
					pf.ProcessorVersion = "v2.0";
					pf.ProcessedAt = DateTimeOffset.UtcNow;
					pf.MetadataJson = JsonSerializer.Serialize<string>(text);
					CancellationToken ct1 = ct;
					ProcessedFile processedFile = await workspace.AddProcessedFileAsync(pf, ct1);
				}
				modelName = (string)null;
			}
			extractedText = await this.ExtractTextAsync(bytes, vf.FileName, stored.MimeType, ct);
			if (string.IsNullOrWhiteSpace(extractedText))
			{
				this._logger.LogWarning("No extractable text for {FileName}", (object)vf.FileName);
				return new IngestionResult()
				{
					CompletedAt = DateTime.UtcNow,
					StoredId = blake3Hash
				};
			}
			extractedText = IngestionPipeline.NormalizeExtractedText(extractedText);
			extractedText = IngestionPipeline.NormalizeLineBreaks(extractedText);
			shapeResult = this._documentShapeDetector.Detect(extractedText);
			this._logger.LogDebug("Shape detected: {Shape} (confidence={Confidence:F2})", (object)shapeResult.Shapes, (object)shapeResult.Confidence);
			await using (MemoryStream derivedStream = new MemoryStream(Encoding.UTF8.GetBytes(extractedText)))
			{
				extractedTextHash = Convert.ToHexString(hasher.ComputeHash((Stream)derivedStream)).ToLowerInvariant();
				derivedStream.Seek(0L, SeekOrigin.Begin);
				if (!await this._files.ExistsAsync("derived", extractedTextHash, ct))
				{
					await this._files.WriteAsync("derived", extractedTextHash, (Stream)derivedStream, ct);
					this._logger.LogDebug("Stored derived text {Hash}", (object)extractedTextHash.Substring(0, 12));
				}
				ChunkingOptions options = ChunkingStrategyFactory.SelectOptionsForShape(shapeResult) with
				{
					FileName = vf.FileName,
					MimeType = stored.MimeType,
					Blake3Hash = blake3Hash
				};
				chunkingResult = this._chunkingFactory.Chunk(extractedText, shapeResult, options);
				this._logger.LogInformation("Chunked into {Count} chunks using {Strategy}", (object)chunkingResult.Chunks.Count, (object)chunkingResult.StrategyName);
				DocumentIngestionMetadata metadata = MetadataExtensions.CreateMetadata(extractedText, shapeResult, chunkingResult, options, "docling");
				IWorkspaceManager workspace1 = this._workspace;
				ProcessedFile pf1 = new ProcessedFile();
				pf1.StoredFileHash = stored.Blake3Hash;
				pf1.DerivedHash = extractedTextHash;
				pf1.ProcessorName = "TextExtraction";
				pf1.ProcessorKind = "extraction";
				pf1.ProcessorVersion = "v2.0";
				pf1.ProcessedAt = DateTimeOffset.UtcNow;
				pf1.MetadataJson = JsonSerializer.Serialize<DocumentIngestionMetadata>(metadata);
				CancellationToken ct2 = ct;
				ProcessedFile processedFile1 = await workspace1.AddProcessedFileAsync(pf1, ct2);
				vectorResult = await this.IndexVectorsAsync(blake3Hash, chunkingResult, vf, stored.MimeType, ct);
				ExtractionResult extractionResult = this._indicatorExtractor.Extract(extractedText);
				IWorkspaceManager workspace2 = this._workspace;
				ProcessedFile pf2 = new ProcessedFile();
				pf2.StoredFileHash = stored.Blake3Hash;
				pf2.DerivedHash = extractedTextHash;
				pf2.ProcessorName = "RegExtraction";
				pf2.ProcessorKind = "extraction";
				pf2.ProcessorVersion = "0.1";
				pf2.ProcessedAt = DateTimeOffset.UtcNow;
				pf2.MetadataJson = JsonSerializer.Serialize<ExtractionResult>(extractionResult);
				CancellationToken ct3 = ct;
				ProcessedFile processedFile2 = await workspace2.AddProcessedFileAsync(pf2, ct3);
				if (this._graphRag != null)
					await this._graphExtractionJob.EnqueueAsync(blake3Hash, extractedTextHash, vf.Id);
				return new IngestionResult()
				{
					StoredId = blake3Hash,
					ChunkCount = chunkingResult.Chunks.Count,
					VectorCount = vectorResult.VectorCount,
					CompletedAt = DateTime.UtcNow
				};
			}
		}
		vf = (VirtualFile)null;
		stored = (StoredFile)null;
		blake3Hash = (string)null;
		bytes = (byte[])null;
		extractedText = (string)null;
		shapeResult = (DocumentShapeResult)null;
	
		extractedTextHash = (string)null;
		chunkingResult = (ChunkingResult)null;
		vectorResult = (VectorIndexResult)null;
		throw null;
	}

	private async Task<string?> ExtractTextAsync(
	  byte[] bytes,
	  string fileName,
	  string mimeType,
	  CancellationToken ct)
	{
		if (mimeType == "application/pdf" || mimeType.Contains("officedocument"))
		{
			ExtractedDocument async = await this._documentRouter.ExtractAsync(bytes, fileName, mimeType, ct);
			this._logger.LogInformation("Document extracted using {Engine} (fallback={Fallback})", (object)async.Engine, (object)async.UsedFallback);
			return async.Text;
		}
		if (mimeType.StartsWith("text/"))
			return Encoding.UTF8.GetString(bytes);
		this._logger.LogInformation("Unsupported type {Mime}; metadata-only ingestion.", (object)mimeType);
		return (string)null;
	}

	private static string NormalizeExtractedText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return text;
		text = text.Normalize(NormalizationForm.FormKC);
		text = text.Replace(' ', ' ').Replace(' ', ' ').Replace(' ', ' ').Replace(' ', ' ');
		text = Regex.Replace(text, "[ ]{2,}", " ");
		text = Regex.Replace(text, "[ \\t]+\\r?$", "", RegexOptions.Multiline);
		return text;
	}

	private static string NormalizeLineBreaks(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return text;
		string[] strArray = text.Split('\n');
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		for (int index = 0; index < strArray.Length; ++index)
		{
			string input = strArray[index].TrimEnd();
			if (index == strArray.Length - 1)
			{
				stringBuilder.AppendLine(input);
				break;
			}
			string str = strArray[index + 1].TrimStart();
			int num = input.EndsWith('.') || input.EndsWith(':') || input.EndsWith(';') || input.EndsWith('?') ? 1 : (input.EndsWith('!') ? 1 : 0);
			bool flag1 = str.Length > 0 && char.IsLower(str[0]);
			bool flag2 = input.TrimStart().StartsWith("-") || input.TrimStart().StartsWith("•") || Regex.IsMatch(input.TrimStart(), "^\\d+(\\.|-)");
			bool flag3 = Regex.IsMatch(input, "^\\s*[A-Z0-9 ._-]{3,}\\s*$");
			if ((!(num == 0 & flag1) || flag2 ? 0 : (!flag3 ? 1 : 0)) != 0)
			{
				stringBuilder.Append(input);
				stringBuilder.Append(' ');
			}
			else
				stringBuilder.AppendLine(input);
		}
		return stringBuilder.ToString();
	}

	private async Task<VectorIndexResult> IndexVectorsAsync(
	  string blake3Hash,
	  ChunkingResult chunkingResult,
	  VirtualFile vf,
	  string mimeType,
	  CancellationToken ct)
	{
		if (!this._embedding.IsReady)
		{
			this._logger.LogWarning("Embedding service not ready, skipping vector indexing");
			return new VectorIndexResult()
			{
				ChunkCount = 0,
				VectorCount = 0
			};
		}
		if (chunkingResult.Chunks.Count == 0)
			return new VectorIndexResult()
			{
				ChunkCount = 0,
				VectorCount = 0
			};
		List<EmbeddingWorkItem> workItems =
			chunkingResult.Chunks
				.Select(chunk =>
				{
					var contentType = chunk.ContentType;

					IReadOnlyDictionary<string, string> metadata =
						new Dictionary<string, string>
						{
							["file_name"] = vf.FileName,
							["mime_type"] = mimeType,
							["content_type"] = contentType.ToString(),
							["section_path"] = chunk.SectionPath ?? "",
							["parent_section"] = chunk.ParentSection ?? ""
						};

					return new EmbeddingWorkItem
					{
						Blake3Hash = blake3Hash,
						ChunkIndex = chunk.Index,
						Text = chunk.OverlapPrefix != null
							? $"{chunk.OverlapPrefix} {chunk.Text}"
							: chunk.Text,
						MaxTokens = 512,
						SemanticType = contentType.ToString().ToLowerInvariant(),
						Metadata = metadata
					};
				})
				.ToList();


		List<ChunkData> chunkData = workItems.Zip<EmbeddingWorkItem, float[], ChunkData>((IEnumerable<float[]>)await this._embedding.EmbedAsync((IReadOnlyList<EmbeddingWorkItem>)workItems, ct), (Func<EmbeddingWorkItem, float[], ChunkData>)((work, embedding) => new ChunkData()
		{
			ChunkIndex = work.ChunkIndex,
			Embedding = embedding,
			Text = work.Text,
			Metadata = new ChunkMetadata()
			{
				FileName = vf.FileName,
				MimeType = mimeType,
				Classification = work.SemanticType,
				IndexedAt = DateTimeOffset.UtcNow,
				WorkspaceId = vf.WorkspaceId,
				VirtualFileId = vf.Id,
				SectionPath = work.Metadata.GetValueOrDefault<string, string>("section_path"),
				ParentSection = work.Metadata.GetValueOrDefault<string, string>("parent_section")
			}
		})).ToList<ChunkData>();
		await this._qdrant.StoreChunksAsync(blake3Hash, chunkData, ct);
		this._logger.LogInformation("Stored {Count} vectors for hash {Hash}", (object)chunkData.Count, (object)blake3Hash.Substring(0, 12));
		return new VectorIndexResult()
		{
			ChunkCount = chunkingResult.Chunks.Count,
			VectorCount = chunkData.Count
		};
	}
}
