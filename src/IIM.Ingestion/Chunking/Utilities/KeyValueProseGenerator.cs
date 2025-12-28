using System.Text;
using System.Text.RegularExpressions;

namespace IIM.Ingestion.Chunking.Utilities;

internal static class KeyValueProseGenerator
{
	private static readonly Regex KeyValueRegex =
		new(@"^\s*([A-Za-z0-9_.\- ]+)\s*[:=]\s*(.+)$", RegexOptions.Compiled);

	public static bool IsKeyValueBlock(string text)
	{
		var lines = text
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.ToList();

		if (lines.Count < 2)
			return false;

		var matches = lines.Count(line => KeyValueRegex.IsMatch(line));

		// Require majority match to avoid false positives
		return matches >= Math.Max(2, lines.Count * 0.6);
	}

	public static string Generate(string text)
	{
		var lines = text
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(l => l.Trim())
			.ToList();

		var keys = new List<string>();

		foreach (var line in lines)
		{
			var match = KeyValueRegex.Match(line);
			if (match.Success)
			{
				keys.Add(match.Groups[1].Value.Trim());
			}
		}

		var sb = new StringBuilder();
		sb.AppendLine("This section contains key-value pairs.");
		sb.AppendLine($"The block contains {keys.Count} entries.");

		const int maxKeysToEmit = 12;

		if (keys.Count > 0)
		{
			sb.Append("The keys present include: ");
			sb.Append(string.Join(", ", keys.Take(maxKeysToEmit)));
			sb.AppendLine(".");
		}

		if (keys.Count > maxKeysToEmit)
		{
			sb.AppendLine("Additional keys are present but not enumerated here.");
		}

		return sb.ToString();
	}
}
