using System.Text.Json;
using System.Text.RegularExpressions;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;

namespace IIM.Api.Services;


public class ToolRegistry : IToolRegistry
{
	private readonly Dictionary<string, Func<IDictionary<string, object?>, Task<string>>> _tools
		= new(StringComparer.OrdinalIgnoreCase);

	private readonly List<AITool> _aiTools = new();

	public ToolRegistry()
	{
		RegisterDefaultTools();
	}

	private void RegisterDefaultTools()
	{
		// Example Tool: Weather
		Register("GetWeather", async args =>
		{
			var city = args.TryGetValue("city", out var v) ? v?.ToString() ?? "unknown" : "unknown";
			return $"Weather for {city}: -2°C, light snow (simulated)";
		});

		_aiTools.Add(AIFunctionFactory.Create(
			GetWeatherFn,
			name: "GetWeather",
			description: "Gets the current weather for a city"));

		// Example Tool: Math
		Register("MathAdd", async args =>
		{
			int a = Convert.ToInt32(args.TryGetValue("a", out var va) ? va : 0);
			int b = Convert.ToInt32(args.TryGetValue("b", out var vb) ? vb : 0);
			return (a + b).ToString();
		});

		_aiTools.Add(AIFunctionFactory.Create(
			MathAddFn,
			name: "MathAdd",
			description: "Add two integers"));
	}

	// Bound attribute versions (ignored except for metadata)
	private static string GetWeatherFn(string city) => $"Weather for {city}.";
	private static string MathAddFn(int a, int b) => (a + b).ToString();

	public void Register(string name, Func<IDictionary<string, object?>, Task<string>> handler)
		=> _tools[name] = handler;

	public async Task<string> InvokeAsync(string name, IDictionary<string, object?>? args)
	{
		if (!_tools.TryGetValue(name, out var fn))
			throw new InvalidOperationException($"Tool '{name}' not found.");
		return await fn(args ?? new Dictionary<string, object?>());
	}

	public IList<AITool> GetAIFunctions() => _aiTools;

	// Robust JSON tool-call detection
	public ToolCall? TryParseToolCall(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			return null;

		// 1. Strip <tool_call> wrappers and Thai/garbage characters
		raw = raw.Replace("<tool_call>", "")
				 .Replace("</tool_call>", "")
				 .Replace("\uFEFF", "")           // BOM
				 .Replace("\u200B", "")           // zero-width space
				 .Trim();

		// 2. Remove any leading non-JSON characters (Phi/Qwen often adds junk)
		int firstBrace = raw.IndexOf('{');
		if (firstBrace < 0)
			return null;

		raw = raw.Substring(firstBrace);

		// 3. Extract balanced JSON object
		string? json = ExtractBalancedJson(raw);
		if (json == null)
			return null;

		// 4. Fix extra braces (common Qwen bug)
		json = FixBraceMismatch(json);

		try
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		

			var toolCall = JsonSerializer.Deserialize<ToolCall>(json, options);
			return toolCall;
		}
		catch
		{
			return null;
		}
	}


	// Remove Thai, stray symbols, invalid bytes, etc.
	private static string Sanitize(string input)
	{
		return Regex.Replace(input, @"[^\u0009\u000A\u000D\u0020-\u007E\u00A0-\u00FF{}:\[\],""A-Za-z0-9._\- ]", "");
	}

	private static string FixBraceMismatch(string json)
	{
		int open = json.Count(c => c == '{');
		int close = json.Count(c => c == '}');

		while (close > open && json.EndsWith("}"))
		{
			json = json.Substring(0, json.Length - 1);
			close--;
		}

		return json;
	}


	private static string? ExtractBalancedJson(string text)
	{
		int depth = 0;
		int start = text.IndexOf('{');
		if (start < 0) return null;

		for (int i = start; i < text.Length; i++)
		{
			if (text[i] == '{') depth++;
			if (text[i] == '}') depth--;

			if (depth == 0)
				return text.Substring(start, i - start + 1);
		}
		return null;
	}


	private static string? TryExtractJson(string text)
	{
		int depth = 0;
		int start = -1;

		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '{')
			{
				if (depth == 0)
					start = i;
				depth++;
			}
			else if (text[i] == '}')
			{
				depth--;
				if (depth == 0 && start != -1)
				{
					var json = text.Substring(start, i - start + 1);
					if (json.Contains("\"name\""))
						return json;
				}
			}
		}
		return null;
	}

    ToolCall? IToolRegistry.TryParseToolCall(string content)
    {
        throw new NotImplementedException();
    }
}

