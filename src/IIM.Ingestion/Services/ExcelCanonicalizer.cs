using System.Text;
using System.Text.Json;

namespace IIM.Ingestion.Services;

/// <summary>
/// Deterministically converts ExcelStructureDetector output to canonical text.
/// This is intentionally conservative and bounded to prevent memory blowups.
/// </summary>
public sealed class ExcelCanonicalizer
{
	public string CanonicalizeJson(string workbookStructureJson, int maxChars = 250_000)
	{
		// Keep it deterministic: JSON in, text out.
		// We don't depend on dynamic runtime state.
		using var doc = JsonDocument.Parse(workbookStructureJson);

		var sb = new StringBuilder(capacity: Math.Min(maxChars, 64_000));

		sb.AppendLine("=== EXCEL WORKBOOK (CANONICAL) ===");

		// Best-effort structure; adapt to your actual WorkbookStructureResult shape
		if (doc.RootElement.TryGetProperty("WorkbookName", out var wbName))
			sb.AppendLine($"Workbook: {wbName.GetString()}");

		if (doc.RootElement.TryGetProperty("Sheets", out var sheets) && sheets.ValueKind == JsonValueKind.Array)
		{
			foreach (var sheet in sheets.EnumerateArray())
			{
				if (sb.Length >= maxChars) break;

				var sheetName = sheet.TryGetProperty("Name", out var n) ? n.GetString() : "Sheet";
				sb.AppendLine();
				sb.AppendLine($"--- Sheet: {sheetName} ---");

				if (sheet.TryGetProperty("Shape", out var shape))
					sb.AppendLine($"Shape: {shape.GetString()}");

				// Regions/Tables
				if (sheet.TryGetProperty("Regions", out var regions) && regions.ValueKind == JsonValueKind.Array)
				{
					int regionIdx = 0;
					foreach (var region in regions.EnumerateArray())
					{
						if (sb.Length >= maxChars) break;

						regionIdx++;
						sb.AppendLine();
						sb.AppendLine($"[Region {regionIdx}]");

						if (region.TryGetProperty("Bounds", out var bounds))
							sb.AppendLine($"Bounds: {bounds}");

						if (region.TryGetProperty("Header", out var header))
							sb.AppendLine($"Header: {header}");

						// Column profiles (names/types)
						if (region.TryGetProperty("Columns", out var cols) && cols.ValueKind == JsonValueKind.Array)
						{
							sb.AppendLine("Columns:");
							foreach (var col in cols.EnumerateArray())
							{
								if (sb.Length >= maxChars) break;

								var colName = col.TryGetProperty("Name", out var cn) ? cn.GetString() : null;
								var colType = col.TryGetProperty("PrimaryType", out var ct) ? ct.GetString() : null;
								sb.AppendLine($"- {colName ?? "(unnamed)"} ({colType ?? "unknown"})");
							}
						}

						// Optional sample rows if present
						if (region.TryGetProperty("SampleRows", out var sampleRows) && sampleRows.ValueKind == JsonValueKind.Array)
						{
							sb.AppendLine("SampleRows:");
							foreach (var row in sampleRows.EnumerateArray())
							{
								if (sb.Length >= maxChars) break;
								sb.AppendLine(row.ToString());
							}
						}
					}
				}
			}
		}

		// Hard cap
		if (sb.Length > maxChars)
			return sb.ToString(0, maxChars) + "\n[TRUNCATED]";

		return sb.ToString();
	}
}
