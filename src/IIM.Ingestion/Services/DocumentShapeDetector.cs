// ═══════════════════════════════════════════════════════════════════════════════
// DOCUMENT SHAPE DETECTOR
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IIM.Shared.Models;

namespace IIM.Ingestion.Services;

/// <summary>
/// Detects structural document shape deterministically at ingestion time.
/// This is NOT semantic classification. It detects observable structure only.
///
/// Designed to support:
/// - RAG control
/// - Evidence extraction
/// - Section-level citations
/// - Query-time strategy selection
/// </summary>
public sealed partial class DocumentShapeDetector
{
	// ──────────────────────────────────────────────────────────────────────────
	// SOURCE-GENERATED REGEX (fast, deterministic)
	// ──────────────────────────────────────────────────────────────────────────

	[GeneratedRegex(@"^(?:#+\s*|[Ss]ection\s+)?(\d+(\.\d+)*)(?:\s+.*)?$", RegexOptions.Multiline)]
	private static partial Regex NumericHeaderRegex();

	[GeneratedRegex(@"^\s*(?:[-•∞*]|\u2022)\s+", RegexOptions.Multiline)]
	private static partial Regex BulletRegex();

	[GeneratedRegex(@"\b(19|20)\d{2}[-/\.](0[1-9]|1[0-2])[-/\.](0[1-9]|[12]\d|3[01])\b")]
	private static partial Regex DateRegex();

	[GeneratedRegex(@"\b\d{2}:\d{2}:\d{2}\b")]
	private static partial Regex TimeRegex();

	[GeneratedRegex(@"^\s*\[[^\]]+\]\s+", RegexOptions.Multiline)]
	private static partial Regex LogPrefixRegex();

	// ──────────────────────────────────────────────────────────────────────────
	// PUBLIC API
	// ──────────────────────────────────────────────────────────────────────────

	public DocumentShapeResult Detect(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return new DocumentShapeResult
			{
				Shapes = DocumentShape.None,
				Confidence = 0f
			};
		}

		var lines = text.Split('\n');
		var evidence = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		int numericHeaders = CountLineMatches(lines, NumericHeaderRegex());
		int bullets = CountLineMatches(lines, BulletRegex());
		int dates = DateRegex().Matches(text).Count;
		int times = TimeRegex().Matches(text).Count;
		int logLines = CountLineMatches(lines, LogPrefixRegex());

		evidence["numericHeaders"] = numericHeaders;
		evidence["bullets"] = bullets;
		evidence["dates"] = dates;
		evidence["timestamps"] = times;
		evidence["logLines"] = logLines;

		DocumentShape shapes = DocumentShape.None;
		float confidence = 0f;

		// ───────────── Sectioned
		if (numericHeaders >= 3)
		{
			shapes |= DocumentShape.Sectioned;
			confidence += 0.25f;
		}

		// ───────────── Versioned (numeric headers + bullet-heavy sections)
		if (numericHeaders >= 3 && bullets >= 5)
		{
			shapes |= DocumentShape.Versioned;
			confidence += 0.25f;
		}

		// ───────────── Chronological
		if (dates >= 3 || times >= 5)
		{
			shapes |= DocumentShape.Chronological;
			confidence += 0.20f;
		}

		// ───────────── List-based
		if (bullets >= 5)
		{
			shapes |= DocumentShape.ListBased;
			confidence += 0.15f;
		}

		// ───────────── Log-like
		if (logLines >= lines.Length * 0.3)
		{
			shapes |= DocumentShape.LogLike;
			confidence += 0.20f;
		}

		// ───────────── Narrative fallback
		if (shapes == DocumentShape.None)
		{
			shapes = DocumentShape.Narrative;
			confidence = 0.40f;
		}

		// ───────────── Section extraction (only if sectioned)
		var sections = shapes.HasFlag(DocumentShape.Sectioned)
			? ExtractSections(text)
			: Array.Empty<DocumentSection>();

		return new DocumentShapeResult
		{
			Shapes = shapes,
			Confidence = Math.Min(1f, confidence),

			HasNumericHeaders = numericHeaders >= 3,
			HeaderPattern = numericHeaders >= 3 ? @"^\d+(\.\d+)*$" : null,
			HasBulletLists = bullets >= 5,
			HasDates = dates >= 3,
			HasTimestamps = times >= 3,

			Sections = sections,
			EvidenceCounts = evidence
		};
	}

	// ──────────────────────────────────────────────────────────────────────────
	// SECTION EXTRACTION (for citations & evidence slicing)
	// ──────────────────────────────────────────────────────────────────────────

	private static IReadOnlyList<DocumentSection> ExtractSections(string text)
	{
		var matches = NumericHeaderRegex().Matches(text);
		var sections = new List<DocumentSection>();

		for (int i = 0; i < matches.Count; i++)
		{
			var header = matches[i].Value.Trim();
			var start = matches[i].Index;
			var end = (i + 1 < matches.Count)
				? matches[i + 1].Index
				: text.Length;

			sections.Add(new DocumentSection
			{
				Id = header,
				Header = header,
				StartOffset = start,
				EndOffset = end
			});
		}

		return sections;
	}

	// ──────────────────────────────────────────────────────────────────────────
	// HELPERS
	// ──────────────────────────────────────────────────────────────────────────

	private static int CountLineMatches(string[] lines, Regex regex)
	{
		int count = 0;
		foreach (var line in lines)
		{
			if (regex.IsMatch(line))
				count++;
		}
		return count;
	}
}



