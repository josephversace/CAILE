using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using IIM.Shared.Models;

namespace IIM.Shared.Dtos;

/// <summary>
/// DTO for displaying ingestion pipeline steps in the UI.
/// Maps from IngestionStepState.
/// </summary>
public class IngestionStepDto
{
	public Guid Id { get; set; }
	public string StepId { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public string? StepVersion { get; set; }

	public string? InputHash { get; set; }
	public string? OutputHash { get; set; }
	public string? ParametersHash { get; set; }

	public IngestionStepStatus Status { get; set; }
	public int AttemptCount { get; set; }
	public bool IsFatal { get; set; }
	public bool IsSkipped { get; set; }
	public bool IsDeferred { get; set; }

	public Dictionary<string, object>? Metadata { get; set; }

	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? StartedAt { get; set; }
	public DateTimeOffset? CompletedAt { get; set; }

	public string? ErrorSummary { get; set; }
	public string? FullStackTrace { get; set; }

	// Computed
	public TimeSpan? Duration =>
		StartedAt.HasValue && CompletedAt.HasValue
			? CompletedAt.Value - StartedAt.Value
			: null;

	public string DurationDisplay
	{
		get
		{
			var d = Duration;
			if (d == null) return "—";
			if (d.Value.TotalMilliseconds < 1000) return $"{d.Value.TotalMilliseconds:F0}ms";
			if (d.Value.TotalSeconds < 60) return $"{d.Value.TotalSeconds:F1}s";
			if (d.Value.TotalMinutes < 60) return $"{d.Value.TotalMinutes:F1}m";
			return $"{d.Value.TotalHours:F1}h";
		}
	}

	public string StatusIcon => Status switch
	{
		IngestionStepStatus.Completed => "✓",
		IngestionStepStatus.Failed => "✗",
		IngestionStepStatus.Skipped => "○",
		IngestionStepStatus.Running => "◐",
		IngestionStepStatus.Pending => "○",
		_ => "?"
	};

	public string StatusClass => Status switch
	{
		IngestionStepStatus.Completed => "status-success",
		IngestionStepStatus.Failed => "status-error",
		IngestionStepStatus.Skipped => "status-skipped",
		IngestionStepStatus.Running => "status-running",
		IngestionStepStatus.Pending => "status-pending",
		_ => ""
	};

	/// <summary>
	/// Creates DTO from database entity.
	/// </summary>
	public static IngestionStepDto FromState(IngestionStepState step)
	{
		var metadata = ParseMetadata(step.MetadataJson);
		var isSkipped = step.Status == IngestionStepStatus.Skipped || IsSkippedFromMetadata(metadata);

		return new IngestionStepDto
		{
			Id = step.Id,
			StepId = step.StepId,
			DisplayName = GetDisplayName(step.StepId),
			StepVersion = step.StepVersion,
			InputHash = step.InputHash,
			OutputHash = step.OutputHash,
			ParametersHash = step.ParametersHash,
			Status = isSkipped ? IngestionStepStatus.Skipped : step.Status,
			AttemptCount = step.AttemptCount,
			IsFatal = step.IsFatal,
			IsSkipped = isSkipped,
			IsDeferred = step.IsDeferred,
			Metadata = metadata,
			CreatedAt = step.CreatedAt,
			StartedAt = step.StartedAt,
			CompletedAt = step.CompletedAt,
			ErrorSummary = step.LastError != null ? ExtractErrorSummary(step.LastError) : null,
			FullStackTrace = step.LastError
		};
	}

	private static Dictionary<string, object>? ParseMetadata(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return null;

		try
		{
			var result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
			if (result == null) return null;

			return result.ToDictionary(
				kvp => kvp.Key,
				kvp => ConvertElement(kvp.Value)
			);
		}
		catch
		{
			return null;
		}
	}

	private static object ConvertElement(JsonElement el) => el.ValueKind switch
	{
		JsonValueKind.String => el.GetString() ?? "",
		JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
		JsonValueKind.True => true,
		JsonValueKind.False => false,
		JsonValueKind.Null => null!,
		_ => el.ToString()
	};

	private static bool IsSkippedFromMetadata(Dictionary<string, object>? metadata)
	{
		if (metadata == null) return false;
		if (metadata.TryGetValue("skipped", out var val))
			return val is bool b && b || val?.ToString()?.ToLower() == "true";
		return false;
	}

	private static string ExtractErrorSummary(string error)
	{
		var firstLine = error.Split('\n').FirstOrDefault() ?? error;
		if (firstLine.Contains(':'))
		{
			var idx = firstLine.IndexOf(':');
			var type = firstLine[..idx].Split('.').Last();
			var msg = firstLine[(idx + 1)..].Trim();
			if (msg.Length > 100) msg = msg[..97] + "...";
			return $"{type}: {msg}";
		}
		return firstLine.Length > 120 ? firstLine[..117] + "..." : firstLine;
	}

	private static readonly Dictionary<string, string> DisplayNames = new()
	{
		["meta.exif.fast"] = "EXIF Metadata",
		["doc.extract.text"] = "Text Extraction",
		["doc.shape.detect"] = "Document Structure",
		["excel.structure.detect"] = "Excel Structure",
		["excel.canonicalize.tabletext"] = "Excel Tables",
		["ai.image.describe"] = "Image Description",
		["ai.text.analysis"] = "AI Text Analysis",
		["ioc.regex.extract"] = "Indicator Extraction",
		["chunk.build"] = "Search Chunks",
		["embed.index.qdrant"] = "Vector Index",
		["web.capture.screenshot"] = "Page Screenshot",
		["web.capture.thumbnail"] = "Page Thumbnail",
		["web.extract.markdown"] = "Web Content",
	};

	private static string GetDisplayName(string stepId)
	{
		if (DisplayNames.TryGetValue(stepId, out var name))
			return name;
		return string.Join(" ", stepId.Split('.').Select(s =>
			s.Length > 0 ? char.ToUpper(s[0]) + s[1..] : s));
	}
}