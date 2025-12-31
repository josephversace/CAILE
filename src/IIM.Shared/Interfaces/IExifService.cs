using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Models;

namespace IIM.Shared.Interfaces
{
	public enum ExifToolProfile
	{
		Fast,
		Deep,
		MediaOnly
	}
	public sealed record ExifToolResult(
		ExifToolProfile Profile,
		string ExifToolVersion,
		string Blake3Hash,
		string SourceFileName,
		JsonDocument RawJson,
		NormalizedExifMetadata? Normalized,
		DateTimeOffset ExecutedAt,
		TimeSpan Duration);


	public interface IExifToolService
	{
		Task<ExifToolResult?> RunAsync(
			byte[] bytes,
			string fileName,
			string blake3Hash,
			ExifToolProfile profile,
			CancellationToken ct);

		NormalizedExifMetadata Normalize(JsonDocument raw);

	}

}
