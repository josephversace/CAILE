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


}

