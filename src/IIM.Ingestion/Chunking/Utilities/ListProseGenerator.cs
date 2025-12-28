using System.Text;

namespace IIM.Ingestion.Chunking.Utilities;

internal static class ListProseGenerator
{
	public static string Generate(string listText)
	{
		var lines = listText
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.Where(IsListItem)
			.ToList();

		if (lines.Count == 0)
			return "This section contains a list.";

		var cleanedItems = lines
			.Select(StripListMarker)
			.Where(i => i.Length > 0)
			.ToList();

		var sb = new StringBuilder();
		sb.AppendLine("This section contains a list.");

		sb.AppendLine($"The list contains {cleanedItems.Count} items.");

		// Include exact labels (bounded)
		const int maxItemsToEmit = 10;

		var emitItems = cleanedItems.Take(maxItemsToEmit).ToList();
		if (emitItems.Count > 0)
		{
			sb.Append("The listed items include: ");
			sb.Append(string.Join(", ", emitItems));
			sb.AppendLine(".");
		}

		if (cleanedItems.Count > maxItemsToEmit)
		{
			sb.AppendLine("Additional items are present but not enumerated here.");
		}

		return sb.ToString();
	}

	private static bool IsListItem(string line)
	{
		if (line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("+ "))
			return true;

		// Ordered lists: "1. ", "2. "
		var dotIndex = line.IndexOf('.');
		return dotIndex > 0 && dotIndex <= 3 && int.TryParse(line[..dotIndex], out _);
	}

	private static string StripListMarker(string line)
	{
		if (line.Length < 2)
			return line;

		// Bullet lists
		if (line[0] is '-' or '*' or '+')
			return line[1..].Trim();

		// Ordered lists
		var dotIndex = line.IndexOf('.');
		if (dotIndex > 0 && dotIndex <= 3)
			return line[(dotIndex + 1)..].Trim();

		return line;
	}
}
