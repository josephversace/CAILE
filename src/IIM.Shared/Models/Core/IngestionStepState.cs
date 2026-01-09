using System;

namespace IIM.Shared.Models;

/// <summary>
/// Tracks ingestion step execution for a stored file, enabling resumable ingestion
/// without repeating completed work.
/// </summary>
public sealed class IngestionStepState
{
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Content-addressed identity of the underlying stored file (BLAKE3).
	/// </summary>
	public string StoredFileHash { get; set; } = "";

	/// <summary>
	/// Optional: workspace context at time of run (useful for auditing / multi-workspace usage).
	/// </summary>
	public Guid? WorkspaceId { get; set; }

	/// <summary>
	/// Optional: virtual file context at time of run.
	/// </summary>
	public Guid? VirtualFileId { get; set; }

	/// <summary>
	/// Canonical step identifier (extensible string key), e.g. "doc.extract.text".
	/// </summary>
	public string StepId { get; set; } = "";

	/// <summary>
	/// Version string for the step implementation/pipeline (e.g. "2.0", or "excel-detector:1.2").
	/// </summary>
	public string StepVersion { get; set; } = "";

	/// <summary>
	/// Hash representing the inputs that produced this result (e.g. extractedTextHash, structureHash, etc.).
	/// </summary>
	public string InputHash { get; set; } = "";

	/// <summary>
	/// Hash representing the output produced by the step (often equals a derived blob hash).
	/// </summary>
	public string? OutputHash { get; set; }

	/// <summary>
	/// Optional: hash of parameters/options that affect determinism (chunking options, model prompt version, etc.).
	/// Keep stable & deterministic.
	/// </summary>
	public string? ParametersHash { get; set; }

	/// <summary>
	/// Free-form json for diagnostics (keep small).
	/// </summary>
	public string? MetadataJson { get; set; }

	public IngestionStepStatus Status { get; set; } = IngestionStepStatus.Pending;

	public int AttemptCount { get; set; }
	public bool IsFatal { get; set; }
	public bool IsDeferred { get; set; }

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

	public DateTimeOffset? StartedAt { get; set; }
	public DateTimeOffset? CompletedAt { get; set; }

	/// <summary>
	/// Last error string (truncate in code).
	/// </summary>
	public string? LastError { get; set; }
}

/// <summary>
/// Minimal status enum.
/// </summary>
public enum IngestionStepStatus
{
	Pending = 0,
	Running = 1,
	Completed = 2,
	Failed = 3,
	Skipped = 4,
	Inconsistent = 5
}
