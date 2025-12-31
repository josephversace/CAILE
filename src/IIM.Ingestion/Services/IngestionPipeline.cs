using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blake3;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class IngestionPipeline : IIngestionPipeline
{
	private readonly IWorkspaceManager _workspace;
	private readonly IFileStore _files;
	private readonly IAIAgentFactory _agentFactory;
	private readonly IExifToolService _exifTool;
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
	  IExifToolService exifToolService,
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
		this._exifTool = exifToolService;
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
			
			
			var exifResult = await _exifTool.RunAsync(
				bytes,
				vf.FileName,
				blake3Hash,
				ExifToolProfile.Fast,
				ct);

			await _workspace.AddProcessedFileAsync(new ProcessedFile
			{
				StoredFileHash = stored.Blake3Hash,
				ProcessorName = "Exif",
				ProcessorKind = "exif (fast)",
				ProcessorVersion = exifResult.ExifToolVersion,
				ProcessedAt = DateTimeOffset.UtcNow,
				MetadataJson = exifResult?.RawJson is not null
					? JsonSerializer.Serialize(exifResult.RawJson)
					: JsonSerializer.Serialize(new
					{
						Status = "Unavailable",
						Reason = "ExifTool could not parse file or returned no metadata"
					})
			}, ct);




			if (stored.MimeType.StartsWith("image/"))
			{
				await ProcessImageAnalysisAsync(stored, bytes, hasher, vf, ct);

				// Images don't have text extraction, so return early
				return new IngestionResult
				{
					StoredId = blake3Hash,
					CompletedAt = DateTime.UtcNow
				};
			}
			else { 

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


		await ProcessTextAnalysisAsync(stored, extractedText, hasher, ct);


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


					//Run Regex Indicator Extraction
					ExtractionResult extractionResult = this._indicatorExtractor.Extract(extractedText, shapeResult);

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

					//if (this._graphRag != null)
					//	await this._graphExtractionJob.EnqueueAsync(blake3Hash, extractedTextHash, vf.Id);


				}

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


	private async Task ProcessImageAnalysisAsync(StoredFile stored, byte[] bytes, Blake3HashAlgorithm hasher, VirtualFile vf, CancellationToken ct)
	{
		const string investigativePrompt = @"
Perform a forensic and investigative analysis of this image. 
Break your response into the following sections:

1. **Text/OCR Extraction**: Transcribe all visible text, including documents, signs, license plates, or screens. Note font types or hand-writing styles.
2. **Key Entities**: Identify people (clothing, identifying features), vehicles (make/model), and specific objects.
3. **Environment & Context**: Describe the setting (indoor/outdoor), lighting, weather, and any geographic clues (architecture, language on signs).
4. **Digital/Technical Artifacts**: Identify if this is a screenshot, a photo of a screen, or an original photo. Note any visible timestamps or UI elements.
5. **Investigative Leads**: List 3-5 specific details that could be used for further pivot-point analysis.";

		var chatClient = await _agentFactory.GetChatClientAsync();
		var modelName = _agentFactory.CurrentChatModel;

		var messages = new List<ChatMessage>
	{
		new(ChatRole.User, investigativePrompt),
		new(ChatRole.User, new List<AIContent>
		{
			new DataContent(bytes, stored.MimeType)
		})
	};

		var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

		if (response?.Text is null)
			return;

		var analysisText = response.Text;
		var derivedHash = Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(analysisText))).ToLowerInvariant();

		// Store the analysis content
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(analysisText));
		await _files.WriteAsync("derived", derivedHash, stream, ct);

		await _workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = stored.Blake3Hash,
			DerivedHash = derivedHash,
			ProcessorName = "ImageDescription",
			ProcessorKind = modelName,
			ProcessorVersion = "v2.0",
			ProcessedAt = DateTimeOffset.UtcNow,
			MetadataJson = JsonSerializer.Serialize(analysisText)
		}, ct);
	// ═══════════════════════════════════════════════════════════════
    // CHUNK & EMBED IMAGE ANALYSIS
    // ═══════════════════════════════════════════════════════════════
    if (_embedding.IsReady)
		{
			var imageChunks = ChunkImageAnalysis(analysisText, stored.Blake3Hash);

			if (imageChunks.Count > 0)
			{
				var workItems = imageChunks.Select(chunk => new EmbeddingWorkItem
				{
					Blake3Hash = stored.Blake3Hash,
					ChunkIndex = chunk.Index,
					Text = chunk.Text,
					MaxTokens = 512,
					SemanticType = chunk.SectionType,
					Metadata = new Dictionary<string, string>
					{
						["file_name"] = vf.FileName,
						["mime_type"] = stored.MimeType,
						["content_type"] = "image_analysis",
						["section_type"] = chunk.SectionType,
						["source_type"] = "image"
					}
				}).ToList();

				var embeddings = await _embedding.EmbedAsync(workItems, ct);

				var chunkData = workItems.Zip(embeddings, (work, embedding) => new ChunkData
				{
					ChunkIndex = work.ChunkIndex,
					Embedding = embedding,
					Text = work.Text,
					Metadata = new ChunkMetadata
					{
						FileName = vf.FileName,
						MimeType = stored.MimeType,
						Classification = "image_analysis",
						IndexedAt = DateTimeOffset.UtcNow,
						WorkspaceId = vf.WorkspaceId,
						VirtualFileId = vf.Id,
						SectionPath = work.Metadata["section_type"],
						ParentSection = "ImageAnalysis"
					}
				}).ToList();

				await _qdrant.StoreChunksAsync(stored.Blake3Hash, chunkData, ct);
				_logger.LogInformation("Stored {Count} image analysis vectors for {FileName}",
					chunkData.Count, vf.FileName);
			}
		}
	}

	/// <summary>
	/// Chunks image analysis by section headers for better semantic retrieval
	/// </summary>
	private List<ImageAnalysisChunk> ChunkImageAnalysis(string analysisText, string blake3Hash)
	{
		var chunks = new List<ImageAnalysisChunk>();
		var sections = new Dictionary<string, string>
		{
			["Text/OCR"] = "ocr",
			["Key Entities"] = "entities",
			["Environment"] = "environment",
			["Digital/Technical"] = "technical",
			["Investigative Leads"] = "leads"
		};

		// Split by section headers (numbered: 1. **Section**:)
		var sectionPattern = new Regex(@"^\d+\.\s*\*\*([^*]+)\*\*", RegexOptions.Multiline);
		var matches = sectionPattern.Matches(analysisText);

		for (int i = 0; i < matches.Count; i++)
		{
			var match = matches[i];
			var sectionName = match.Groups[1].Value.Trim().TrimEnd(':');

			// Find section type
			var sectionType = sections
				.FirstOrDefault(s => sectionName.Contains(s.Key, StringComparison.OrdinalIgnoreCase))
				.Value ?? "general";

			// Get section content (from this header to next header or end)
			var startIndex = match.Index;
			var endIndex = i < matches.Count - 1 ? matches[i + 1].Index : analysisText.Length;
			var sectionContent = analysisText[startIndex..endIndex].Trim();

			// Skip empty sections
			if (sectionContent.Length < 50)
				continue;

			chunks.Add(new ImageAnalysisChunk
			{
				Index = i,
				Text = sectionContent,
				SectionType = sectionType,
				SectionName = sectionName
			});
		}

		// If no sections found, chunk the whole thing
		if (chunks.Count == 0 && analysisText.Length > 50)
		{
			chunks.Add(new ImageAnalysisChunk
			{
				Index = 0,
				Text = analysisText,
				SectionType = "full_analysis",
				SectionName = "Image Analysis"
			});
		}

		return chunks;
	}

	private class ImageAnalysisChunk
	{
		public int Index { get; set; }
		public string Text { get; set; } = "";
		public string SectionType { get; set; } = "";
		public string SectionName { get; set; } = "";
	}

	private async Task ProcessTextAnalysisAsync(StoredFile stored, string extractedText, Blake3HashAlgorithm hasher, CancellationToken ct)
	{
		// Truncate if too long (context window limits)
		const int maxChars = 100_000;
		var textForAnalysis = extractedText.Length > maxChars
			? extractedText[..maxChars] + "\n\n[TRUNCATED - Document continues...]"
			: extractedText;

		string analysisPrompt = @"
You are a forensic analyst examining a document. Analyze this document thoroughly and provide your findings in the following structure:

## 1. Document Classification
- **Type**: (e.g., Police Report, Financial Record, Email Thread, Chat Log, Legal Document, Intelligence Report, ESP/CyberTipline Report, etc.)
- **Source**: Identify the originating organization/system if apparent
- **Date Range**: Any dates mentioned or time period covered
- **Classification/Sensitivity**: Note any markings or implied sensitivity level

## 2. Executive Summary
Provide a 2-3 sentence overview of what this document contains and its significance.

## 3. Key Entities Identified
Extract and categorize:
- **People**: Names, roles, relationships, identifying information
- **Organizations**: Companies, agencies, platforms mentioned
- **Locations**: Addresses, cities, countries, IP geolocations
- **Accounts/Identifiers**: Usernames, email addresses, phone numbers, IPs, device IDs
- **Financial**: Account numbers, transactions, amounts, cryptocurrency addresses

## 4. Timeline of Events
List key events in chronological order with dates/times if available.

## 5. Critical Findings
What are the 3-5 most important facts or findings an investigator should know immediately?

## 6. Red Flags & Anomalies
Note any inconsistencies, suspicious patterns, or items requiring follow-up.

## 7. Investigative Leads
Suggest 3-5 specific next steps or pivot points for further investigation.

## 8. Related Indicators (IoCs)
List any technical indicators that should be searched/correlated:
- IP addresses
- Domains/URLs
- Email addresses
- Hashes
- Usernames across platforms

Analyze the following document:

---
" + textForAnalysis;

		var chatClient = await _agentFactory.GetChatClientAsync();
		var modelName = _agentFactory.CurrentChatModel;

		var messages = new List<ChatMessage>
	{
		new(ChatRole.User, analysisPrompt)
	};

		var response = await chatClient.GetResponseAsync(messages, cancellationToken: ct);

		if (response?.Text is null)
			return;

		var analysisText = response.Text;
		var derivedHash = Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(analysisText))).ToLowerInvariant();

		// Store the analysis content
		await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(analysisText));
		await _files.WriteAsync("derived", derivedHash, stream, ct);

		await _workspace.AddProcessedFileAsync(new ProcessedFile
		{
			StoredFileHash = stored.Blake3Hash,
			DerivedHash = derivedHash,
			ProcessorName = "TextAnalysis",
			ProcessorKind = modelName,
			ProcessorVersion = "v1.0",
			ProcessedAt = DateTimeOffset.UtcNow,
			MetadataJson = JsonSerializer.Serialize(new TextAnalysisMetadata
			{
				DocumentLength = extractedText.Length,
				WasTruncated = extractedText.Length > maxChars,
				Preview = analysisText.Length > 1000 ? analysisText[..1000] : analysisText
			})
		}, ct);
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
