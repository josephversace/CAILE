// ═══════════════════════════════════════════════════════════════════════════════
// INGESTION PIPELINE V2
// ═══════════════════════════════════════════════════════════════════════════════
//
// Updated ingestion pipeline with:
//   - Shape-aware chunking
//   - Rich metadata for query-time decisions
//   - Section-level tracking for citations
//
// ═══════════════════════════════════════════════════════════════════════════════

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Extensions;
using IIM.Ingestion.Indicators;
using IIM.Ingestion.Interfaces;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services;

public sealed class IngestionPipeline : IIngestionPipeline
{
    private readonly IWorkspaceManager _workspace;
    private readonly IFileStore _files;
    private readonly IDoclingService _docling;
    private readonly DocumentShapeDetector _documentShapeDetector;
    private readonly ChunkingStrategyFactory _chunkingFactory;
    private readonly IMultimodalVisionService _vision;
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
        IDoclingService docling,
        DocumentShapeDetector documentShapeDetector,
        ChunkingStrategyFactory chunkingFactory,
        IMultimodalVisionService vision,
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
        _workspace = workspace;
        _files = files;
        _docling = docling;
        _documentShapeDetector = documentShapeDetector;
        _chunkingFactory = chunkingFactory;
        _vision = vision;
        _graphRag = graphRag;
        _graphExtractionJob = graphExtractionJob;
        _embedding = embedding;
        _qdrant = qdrant;
        _caileConfig = caileConfig;
        _documentRouter = documentRouter;
        _indicatorExtractor = indicatorExtractor;
        _logger = logger;
        _entityLinking = entityLinking;
    }

    public async Task<IngestionResult> IngestAsync(Guid virtualFileId, CancellationToken ct)
    {
        using var hasher = new Blake3.Blake3HashAlgorithm();

        // ════════════════════════════════════════════════════════════════════
        // 1. LOAD FILE REFERENCES
        // ════════════════════════════════════════════════════════════════════

        var vf = await _workspace.GetVirtualFileByIdAsync(virtualFileId, ct)
            ?? throw new InvalidOperationException($"VirtualFile {virtualFileId} not found.");

        var stored = vf.StoredFile
            ?? throw new InvalidOperationException("StoredFile missing.");

        var blake3Hash = stored.Blake3Hash;

        _logger.LogInformation(
            "Ingesting {FileName} [{Hash}] (Pipeline v{Version})",
            vf.FileName, blake3Hash[..12], PipelineVersion);

        // ════════════════════════════════════════════════════════════════════
        // 2. CHECK FOR DEDUPLICATION
        // ════════════════════════════════════════════════════════════════════

        if (await _qdrant.ExistsAsync(blake3Hash, ct))
        {
            _logger.LogInformation(
                "Hash {Hash} already embedded. Attaching to workspace.",
                blake3Hash[..12]);

            await _qdrant.AttachFileToExistingChunksAsync(
                blake3Hash,
                vf.WorkspaceId,
                vf.Id,
                ct);

            return new IngestionResult
            {
                StoredId = blake3Hash,
                Deduplicated = true,
                CompletedAt = DateTime.UtcNow
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // 3. EXTRACT TEXT
        // ════════════════════════════════════════════════════════════════════

        var bytes = await _files.ReadAsync(stored.Bucket, stored.StoragePath, ct);
        var extractedText = await ExtractTextAsync(bytes, vf.FileName, stored.MimeType, ct);

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.LogWarning("No extractable text for {FileName}", vf.FileName);
            return new IngestionResult
            {
                CompletedAt = DateTime.UtcNow,
                StoredId = blake3Hash
            };
        }

        // Normalize text
        extractedText = NormalizeExtractedText(extractedText);
        extractedText = NormalizeLineBreaks(extractedText);

        // ════════════════════════════════════════════════════════════════════
        // 4. DETECT SHAPE
        // ════════════════════════════════════════════════════════════════════

        var shapeResult = _documentShapeDetector.Detect(extractedText);

        _logger.LogDebug(
            "Shape detected: {Shape} (confidence={Confidence:F2})",
            shapeResult.Shapes, shapeResult.Confidence);

        // ════════════════════════════════════════════════════════════════════
        // 5. STORE EXTRACTED TEXT (full text for deterministic retrieval)
        // ════════════════════════════════════════════════════════════════════

        var extractedBytes = Encoding.UTF8.GetBytes(extractedText);
        await using var derivedStream = new MemoryStream(extractedBytes);

        var hashBytes = hasher.ComputeHash(derivedStream);
        var extractedTextHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        derivedStream.Seek(0, SeekOrigin.Begin);

        if (!await _files.ExistsAsync("derived", extractedTextHash, ct))
        {
            await _files.WriteAsync("derived", extractedTextHash, derivedStream, ct);
            _logger.LogDebug("Stored derived text {Hash}", extractedTextHash[..12]);
        }

        // ════════════════════════════════════════════════════════════════════
        // 6. CHUNK USING SHAPE-AWARE STRATEGY
        // ════════════════════════════════════════════════════════════════════

        var chunkingOptions = ChunkingStrategyFactory.SelectOptionsForShape(shapeResult);
        chunkingOptions = chunkingOptions with
        {
            FileName = vf.FileName,
            MimeType = stored.MimeType,
            Blake3Hash = blake3Hash
        };

        var chunkingResult = _chunkingFactory.Chunk(extractedText, shapeResult, chunkingOptions);

        _logger.LogInformation(
            "Chunked into {Count} chunks using {Strategy}",
            chunkingResult.Chunks.Count,
            chunkingResult.StrategyName);

        // ════════════════════════════════════════════════════════════════════
        // 7. STORE METADATA
        // ════════════════════════════════════════════════════════════════════

        var metadata = MetadataExtensions.CreateMetadata(
            extractedText,
            shapeResult,
            chunkingResult,
            chunkingOptions,
            "docling");

        await _workspace.AddProcessedFileAsync(
            new ProcessedFile
            {
                StoredFileHash = stored.Blake3Hash,
                DerivedHash = extractedTextHash,
                ProcessorName = "TextExtraction",
                ProcessorKind = "extraction",
                ProcessorVersion = $"v{PipelineVersion}",
                ProcessedAt = DateTimeOffset.UtcNow,
                MetadataJson = JsonSerializer.Serialize(metadata)
            },
            ct);

        // ════════════════════════════════════════════════════════════════════
        // 8. EMBED AND INDEX VECTORS
        // ════════════════════════════════════════════════════════════════════

        var vectorResult = await IndexVectorsAsync(
            blake3Hash,
            chunkingResult,
            vf,
            stored.MimeType,
            ct);

        // ════════════════════════════════════════════════════════════════════
        // 9. EXTRACT INDICATORS (IOCs)
        // ════════════════════════════════════════════════════════════════════

        var extracted = _indicatorExtractor.Extract(extractedText);

        await _workspace.AddProcessedFileAsync(
            new ProcessedFile
            {
                StoredFileHash = stored.Blake3Hash,
                DerivedHash = extractedTextHash,
                ProcessorName = "RegExtraction",
                ProcessorKind = "extraction",
                ProcessorVersion = "0.1",
                ProcessedAt = DateTimeOffset.UtcNow,
                MetadataJson = JsonSerializer.Serialize(extracted)
            },
            ct);

        // ════════════════════════════════════════════════════════════════════
        // 10. QUEUE GRAPH EXTRACTION (best-effort)
        // ════════════════════════════════════════════════════════════════════

        if (_graphRag != null)
        {
            await _graphExtractionJob.EnqueueAsync(blake3Hash, extractedTextHash, vf.Id);
        }

        return new IngestionResult
        {
            StoredId = blake3Hash,
            ChunkCount = chunkingResult.Chunks.Count,
            VectorCount = vectorResult.VectorCount,
            CompletedAt = DateTime.UtcNow
        };
    }

    // ────────────────────────────────────────────────────────────────────────────
    // TEXT EXTRACTION
    // ────────────────────────────────────────────────────────────────────────────

    private async Task<string?> ExtractTextAsync(
        byte[] bytes,
        string fileName,
        string mimeType,
        CancellationToken ct)
    {
        if (mimeType.StartsWith("image/"))
        {
            return await HandleImageAsync(bytes, ct);
        }

        if (mimeType == "application/pdf" || mimeType.Contains("officedocument"))
        {
            var extracted = await _documentRouter.ExtractAsync(bytes, fileName, mimeType, ct);

            _logger.LogInformation(
                "Document extracted using {Engine} (fallback={Fallback})",
                extracted.Engine,
                extracted.UsedFallback);

            return extracted.Text;
        }

        if (mimeType.StartsWith("text/"))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        _logger.LogInformation("Unsupported type {Mime}; metadata-only ingestion.", mimeType);
        return null;
    }

    private async Task<string?> HandleImageAsync(byte[] bytes, CancellationToken ct)
    {
        if (!_vision.IsReady)
            return null;

        return await _vision.AnalyzeImageAsync(
            "Extract all visible text and investigative details.",
            bytes,
            ct);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // TEXT NORMALIZATION
    // ────────────────────────────────────────────────────────────────────────────

    private static string NormalizeExtractedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        text = text.Normalize(NormalizationForm.FormKC);

        text = text
            .Replace('\u00A0', ' ')
            .Replace('\u2007', ' ')
            .Replace('\u2009', ' ')
            .Replace('\u202F', ' ');

        text = Regex.Replace(text, @"[ ]{2,}", " ");
        text = Regex.Replace(text, @"[ \t]+\r?$", "", RegexOptions.Multiline);

        return text;
    }

    private static string NormalizeLineBreaks(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var lines = text.Split('\n');
        var sb = new StringBuilder(text.Length);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();

            if (i == lines.Length - 1)
            {
                sb.AppendLine(line);
                break;
            }

            var next = lines[i + 1].TrimStart();

            bool endsWithSentencePunctuation =
                line.EndsWith('.') || line.EndsWith(':') ||
                line.EndsWith(';') || line.EndsWith('?') ||
                line.EndsWith('!');

            bool nextStartsLowercase =
                next.Length > 0 && char.IsLower(next[0]);

            bool looksLikeList =
                line.TrimStart().StartsWith("-") ||
                line.TrimStart().StartsWith("•") ||
                Regex.IsMatch(line.TrimStart(), @"^\d+(\.|-)");

            bool looksLikeHeader =
                Regex.IsMatch(line, @"^\s*[A-Z0-9 ._-]{3,}\s*$");

            bool shouldMerge =
                !endsWithSentencePunctuation &&
                nextStartsLowercase &&
                !looksLikeList &&
                !looksLikeHeader;

            if (shouldMerge)
            {
                sb.Append(line);
                sb.Append(' ');
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // VECTOR INDEXING
    // ────────────────────────────────────────────────────────────────────────────

    private async Task<VectorIndexResult> IndexVectorsAsync(
        string blake3Hash,
        ChunkingResult chunkingResult,
        VirtualFile vf,
        string mimeType,
        CancellationToken ct)
    {
        if (!_embedding.IsReady)
        {
            _logger.LogWarning("Embedding service not ready, skipping vector indexing");
            return new VectorIndexResult { ChunkCount = 0, VectorCount = 0 };
        }

        if (chunkingResult.Chunks.Count == 0)
        {
            return new VectorIndexResult { ChunkCount = 0, VectorCount = 0 };
        }

        // Convert DocumentChunks to EmbeddingWorkItems
        var workItems = chunkingResult.Chunks.Select(chunk => new EmbeddingWorkItem
        {
            Blake3Hash = blake3Hash,
            ChunkIndex = chunk.Index,
            Text = chunk.OverlapPrefix != null
                ? $"{chunk.OverlapPrefix} {chunk.Text}"
                : chunk.Text,
            MaxTokens = 512,
            SemanticType = chunk.ContentType.ToString().ToLowerInvariant(),
            Metadata = new Dictionary<string, string>
            {
                ["file_name"] = vf.FileName,
                ["mime_type"] = mimeType,
                ["content_type"] = chunk.ContentType.ToString(),
                ["section_path"] = chunk.SectionPath ?? "",
                ["parent_section"] = chunk.ParentSection ?? ""
            }
        }).ToList();

        // Embed
        var embeddings = await _embedding.EmbedAsync(workItems, ct);

        // Prepare chunk data for Qdrant
        var chunkData = workItems.Zip(embeddings, (work, embedding) => new ChunkData
        {
            ChunkIndex = work.ChunkIndex,
            Embedding = embedding,
            Text = work.Text,
            Metadata = new ChunkMetadata
            {
                FileName = vf.FileName,
                MimeType = mimeType,
                Classification = work.SemanticType,
                IndexedAt = DateTimeOffset.UtcNow,
                WorkspaceId = vf.WorkspaceId,
                VirtualFileId = vf.Id,
                // Store section info for citations
                SectionPath = work.Metadata.GetValueOrDefault("section_path"),
                ParentSection = work.Metadata.GetValueOrDefault("parent_section")
            }
        }).ToList();

        await _qdrant.StoreChunksAsync(blake3Hash, chunkData, ct);

        _logger.LogInformation(
            "Stored {Count} vectors for hash {Hash}",
            chunkData.Count,
            blake3Hash[..12]);

        return new VectorIndexResult
        {
            ChunkCount = chunkingResult.Chunks.Count,
            VectorCount = chunkData.Count
        };
    }
}
