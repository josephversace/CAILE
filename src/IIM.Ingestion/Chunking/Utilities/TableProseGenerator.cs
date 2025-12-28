using System;
using System.Collections.Generic;
using System.Text;


	namespace IIM.Ingestion.Chunking.Utilities;

internal static class TableProseGenerator
{
	public static string Generate(string tableText)
	{
		var lines = tableText
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.ToList();

		if (lines.Count < 2)
			return "This section contains a table with limited structure.";

		var headerLine = lines[0];
		var separatorLine = lines[1];

		if (!separatorLine.Contains('|') || !separatorLine.Contains('-'))
			return "This section contains a table.";

		var headers = headerLine
			.Split('|', StringSplitOptions.RemoveEmptyEntries)
			.Select(h => h.Trim())
			.Where(h => h.Length > 0)
			.ToList();

		var rowCount = Math.Max(0, lines.Count - 2);

		var sb = new StringBuilder();
		sb.AppendLine("This section contains a table.");

		if (headers.Count > 0)
		{
			sb.Append("The table includes the following columns: ");
			sb.Append(string.Join(", ", headers));
			sb.AppendLine(".");
		}

		sb.AppendLine($"The table contains {rowCount} data rows.");

		return sb.ToString();
	}
}
