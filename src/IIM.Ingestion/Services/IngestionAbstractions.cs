using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Blake3;
using IIM.Ingestion.Chunking;
using IIM.Ingestion.Models;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace IIM.Ingestion.Services
{
	public sealed record IngestionRunOptions
	{
		public IReadOnlyList<string>? OnlySteps { get; init; }
		public IReadOnlyList<string>? AdditionalSteps { get; init; }

		public bool IncludeDependencies { get; init; } = true;
		public bool Force { get; init; } = false;
		public bool ContinueOnError { get; init; } = true;

		public static IngestionRunOptions Default => new();
	}

	public interface IIngestionStep
	{
		string Id { get; }
		string Version { get; }
		IReadOnlyList<string> DependsOn { get; }
		bool IsFatal { get; }

		bool RequiresBytes { get; }

		ValueTask<(string InputHash, string? ParametersHash)> GetIdentityAsync(IngestionStepContext ctx, CancellationToken ct);
		Task<(string? OutputHash, string? MetadataJson)> ExecuteAsync(IngestionStepContext ctx, CancellationToken ct);
		Task<bool> VerifyAsync(IngestionStepContext ctx, string? outputHash, CancellationToken ct);
	}

	public sealed class IngestionStepContext
	{
		public required VirtualFile VirtualFile { get; init; }
		public required StoredFile StoredFile { get; init; }
		public required Blake3HashAlgorithm Hasher { get; init; }

		public required IWorkspaceManager Workspace { get; init; }
		public required IFileStore Files { get; init; }
		public required ILogger Logger { get; init; }

		// pipeline services used by steps
		public required IExifToolService ExifTool { get; init; }
		public required DocumentExtractionRouter DocumentRouter { get; init; }
		public required DocumentShapeDetector ShapeDetector { get; init; }
		public required ChunkingStrategyFactory ChunkingFactory { get; init; }
		public required IEmbeddingService Embedding { get; init; }
		public required IQdrantService Qdrant { get; init; }
		public required IndicatorExtractor IndicatorExtractor { get; init; }
		public required IAIAgentFactory AgentFactory { get; init; }
		public required ExcelStructureDetector ExcelDetector { get; init; }
		public required ExcelCanonicalizer ExcelCanonicalizer { get; init; }

		// Lazy bytes: steps call GetBytesAsync() when needed
		private byte[]? _bytes;
		public byte[]? Bytes => _bytes;

		public required Func<CancellationToken, Task<byte[]>> ReadBytesAsync { get; init; }

		public async ValueTask<byte[]> GetBytesAsync(CancellationToken ct)
			=> _bytes ??= await ReadBytesAsync(ct);

		// in-run scratch (not persisted)
		public Dictionary<string, object> Bag { get; } = new();

		public CancellationTokenSource StopCts { get; init; }

		public void RequestStop() => StopCts.Cancel();
	}

	/// <summary>
	/// Public entry point callers use. Null options => default.
	/// </summary>
	public interface IIngestionRunner
	{
		Task<IngestionResult> RunAsync(Guid virtualFileId, IngestionRunOptions? options, CancellationToken ct);
	}

	internal static class StepIO
	{
		public const string DerivedCollection = "derived";

		public static bool IsXlsx(string mimeType) =>
			mimeType.Equals("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
				StringComparison.OrdinalIgnoreCase);

		public static bool IsImage(string mimeType) =>
			mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

		public static string HashUtf8(Blake3HashAlgorithm hasher, string s)
			=> Convert.ToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

		public static async Task EnsureDerivedAsync(IFileStore files, string hash, byte[] bytes, CancellationToken ct)
		{
			if (!await files.ExistsAsync(DerivedCollection, hash, ct))
			{
				await using var ms = new MemoryStream(bytes, writable: false);
				await files.WriteAsync(DerivedCollection, hash, ms, ct);
			}
		}

		public static async Task<string?> GetLatestDerivedHashAsync(
			IWorkspaceManager ws,
			string storedHash,
			string processorName,
			CancellationToken ct)
		{
			var hashes = await ws.GetDerivedHashForProcessedFile(storedHash, processorName, latestOnly: true, ct);
			if (hashes.Count == 0) return null;
			return string.IsNullOrWhiteSpace(hashes[0]) ? null : hashes[0];
		}

		// Best text for downstream:
		// - image: ImageDescription
		// - xlsx: ExcelCanonical
		// - else: TextExtraction
		public static async Task<string?> GetBestTextHashAsync(IngestionStepContext ctx, CancellationToken ct)
		{
			var storedHash = ctx.StoredFile.Blake3Hash;

			if (IsImage(ctx.StoredFile.MimeType))
				return await GetLatestDerivedHashAsync(ctx.Workspace, storedHash, "ImageDescription", ct);

			if (IsXlsx(ctx.StoredFile.MimeType))
			{
				var h = await GetLatestDerivedHashAsync(ctx.Workspace, storedHash, "ExcelCanonical", ct);
				if (!string.IsNullOrWhiteSpace(h)) return h;
			}

			return await GetLatestDerivedHashAsync(ctx.Workspace, storedHash, "TextExtraction", ct);
		}

		public static async Task<string?> ReadDerivedTextAsync(IFileStore files, string? hash, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(hash)) return null;
			var bytes = await files.ReadAsync(DerivedCollection, hash, ct);
			return Encoding.UTF8.GetString(bytes);
		}

		public static string NormalizeExtractedText(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;

			text = text.Normalize(NormalizationForm.FormKC);
			text = text.Replace('\u00A0', ' ').Replace('\u2007', ' ').Replace('\u2009', ' ').Replace('\u202F', ' ');
			text = Regex.Replace(text, "[ ]{2,}", " ");
			text = Regex.Replace(text, "[ \\t]+\\r?$", "", RegexOptions.Multiline);
			return text;
		}

		public static string NormalizeLineBreaks(string text)
		{
			if (string.IsNullOrWhiteSpace(text)) return text;

			var lines = text.Split('\n');
			var sb = new StringBuilder(text.Length);

			for (int i = 0; i < lines.Length; i++)
			{
				var input = lines[i].TrimEnd();

				if (i == lines.Length - 1)
				{
					sb.AppendLine(input);
					break;
				}

				var next = lines[i + 1].TrimStart();

				bool endsSentence = input.EndsWith('.') || input.EndsWith(':') || input.EndsWith(';') || input.EndsWith('?') || input.EndsWith('!');
				bool nextIsLower = next.Length > 0 && char.IsLower(next[0]);
				bool looksList = input.TrimStart().StartsWith("-") || input.TrimStart().StartsWith("*") || Regex.IsMatch(input.TrimStart(), "^\\d+(\\.|-)");
				bool looksHeader = Regex.IsMatch(input, "^\\s*[A-Z0-9 ._-]{3,}\\s*$");

				if (!endsSentence && nextIsLower && !looksList && !looksHeader)
				{
					sb.Append(input);
					sb.Append(' ');
				}
				else
				{
					sb.AppendLine(input);
				}
			}

			return sb.ToString();
		}
	}
}
