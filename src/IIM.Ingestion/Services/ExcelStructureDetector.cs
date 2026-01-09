using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IIM.Ingestion.Services
{
	/// <summary>
	/// Deterministic, auditable Excel (.xlsx) structure detection.
	/// Structural detection only: no semantic inference, no embeddings.
	/// </summary>
	public sealed class ExcelStructureDetector
	{
		/// <summary>
		/// Detect structure from an XLSX file path.
		/// Uses a stream internally; caller does not control file lifetime.
		/// </summary>
		public WorkbookStructureResult Detect(
			string xlsxPath,
			ExcelStructureDetectorOptions? options = null,
			IStructureAuditSink? auditSink = null,
			CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(xlsxPath))
				throw new ArgumentException("xlsxPath is required.", nameof(xlsxPath));

			using var fs = File.OpenRead(xlsxPath);
			return Detect(fs, Path.GetFileName(xlsxPath), options, auditSink, cancellationToken);
		}

		/// <summary>
		/// Detect structure from an XLSX stream (preferred for sandboxing / controlled disposal).
		/// </summary>
		public WorkbookStructureResult Detect(
			Stream stream,
			string sourceName,
			ExcelStructureDetectorOptions? options = null,
			IStructureAuditSink? auditSink = null,
			CancellationToken cancellationToken = default)
			=> ExcelStructureDetectorInternal.Detect(stream, sourceName, options, auditSink, cancellationToken);

		/// <summary>
		/// Async wrapper (CPU-bound). Useful for UI / server calls.
		/// </summary>
		public Task<WorkbookStructureResult> DetectAsync(
			string xlsxPath,
			ExcelStructureDetectorOptions? options = null,
			IStructureAuditSink? auditSink = null,
			CancellationToken cancellationToken = default)
			=> Task.Run(() => Detect(xlsxPath, options, auditSink, cancellationToken), cancellationToken);

		/// <summary>
		/// Async wrapper for stream input.
		/// </summary>
		public Task<WorkbookStructureResult> DetectAsync(
			Stream stream,
			string sourceName,
			ExcelStructureDetectorOptions? options = null,
			IStructureAuditSink? auditSink = null,
			CancellationToken cancellationToken = default)
			=> Task.Run(() => Detect(stream, sourceName, options, auditSink, cancellationToken), cancellationToken);
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Options
	// ──────────────────────────────────────────────────────────────────────────

	public sealed record ExcelStructureDetectorOptions(
		int MinRegionRows,
		int MinRegionCols,
		int MinRegionCells,
		int HeaderSearchMaxRowsFromTop,
		double HeaderScoreThreshold,
		int ColumnProfileSampleRows,
		int BlankRowSplitGap,
		double EffectiveBlankRowThreshold,
		bool EmitHeaderCandidateScores,
		bool PreferExplicitTables,
		bool ConsiderAutoFilterAsHeaderHint)
	{
		public static ExcelStructureDetectorOptions Default => new(
			MinRegionRows: 2,
			MinRegionCols: 2,
			MinRegionCells: 8,
			HeaderSearchMaxRowsFromTop: 25,
			HeaderScoreThreshold: 0.65,
			ColumnProfileSampleRows: 200,
			BlankRowSplitGap: 2,
			EffectiveBlankRowThreshold: 0.10,  // <10% filled counts as "blank" for splitting
			EmitHeaderCandidateScores: true,
			PreferExplicitTables: true,
			ConsiderAutoFilterAsHeaderHint: true
		);
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Public Result Models
	// ──────────────────────────────────────────────────────────────────────────

	public sealed record WorkbookStructureResult(
		string SourceName,
		UsedRange UsedRange,
		IReadOnlyList<SheetStructureResult> Sheets,
		DetectionDiagnostics Diagnostics);

	public sealed record SheetStructureResult(
		string SheetName,
		SheetShape Shape,
		SheetProfile Profile,
		IReadOnlyList<TableStructureResult> Tables);

	public enum SheetShape
	{
		Unknown = 0,
		Tabular,
		MultiTable,
		Pivot,
		Calculation,
		FormLike,
		FreeText,
		Visual
	}

	public sealed record SheetProfile(
		UsedRange UsedRange,
		int NonEmptyCells,
		double FormulaRatio,
		int HiddenRowCount,
		int HiddenColumnCount,
		bool HasPivotLikeParts,
		bool HasExplicitTables,
		bool HasAutoFilter);

	public sealed record TableStructureResult(
		string SheetName,
		CellRange Range,
		HeaderInfo Header,
		IReadOnlyList<ColumnProfile> Columns,
		TableKind Kind,
		ConfidenceScore Confidence);

	public enum TableKind
	{
		Unknown = 0,
		ExplicitExcelTable,
		Table,
		LogLike,
		FormSection,
		UnstructuredBlock
	}

	public sealed record HeaderInfo(
		int StartRow,
		int EndRow,
		double Score)
	{
		public bool Exists => StartRow > 0;
	}

	public sealed record ColumnProfile(
		int Index,
		string Name,
		ColumnDataType PrimaryType,
		double PrimaryTypeRatio,
		IReadOnlyDictionary<ColumnDataType, int> TypeDistribution,
		int NullCount,
		int UniqueCount);

	public enum ColumnDataType
	{
		Unknown = 0,
		String,
		Number,
		DateTime,
		Boolean,
		Mixed
	}

	public sealed record ConfidenceScore(
		double Region,
		double Header,
		double ColumnStability,
		double Overall);

	public sealed record UsedRange(
		int MinRow,
		int MinCol,
		int MaxRow,
		int MaxCol,
		int NonEmptyCells)
	{
		public bool IsEmpty => NonEmptyCells <= 0 || MaxRow <= 0 || MaxCol <= 0;
		public int RowCount => IsEmpty ? 0 : (MaxRow - MinRow + 1);
		public int ColCount => IsEmpty ? 0 : (MaxCol - MinCol + 1);
	}

	public sealed record CellRange(int MinRow, int MinCol, int MaxRow, int MaxCol)
	{
		public int RowCount => MaxRow - MinRow + 1;
		public int ColCount => MaxCol - MinCol + 1;

		public bool Overlaps(CellRange other) =>
			!(other.MaxRow < MinRow || other.MinRow > MaxRow || other.MaxCol < MinCol || other.MinCol > MaxCol);
	}

	public sealed record DetectionDiagnostics
	{
		public TimeSpan Duration { get; set; }
		public int CellsProcessed { get; set; }
		public int RegionsFound { get; set; }
		public List<string> Warnings { get; } = new();
	}

	// ──────────────────────────────────────────────────────────────────────────
	// Audit
	// ──────────────────────────────────────────────────────────────────────────

	public sealed record StructureAuditEvent(
		DateTime TimestampUtc,
		string Sheet,
		string EventType,
		string Message,
		object Details);

	public interface IStructureAuditSink
	{
		void Write(StructureAuditEvent evt);
	}

	public sealed class NullStructureAuditSink : IStructureAuditSink
	{
		public void Write(StructureAuditEvent evt) { }
	}
}
