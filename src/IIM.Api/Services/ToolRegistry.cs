using IIM.Application.Urls;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace IIM.Api.Services;

public class ToolRegistry : IToolRegistry
{
	private readonly Dictionary<string, Func<IDictionary<string, object?>, Task<string>>> _tools
		= new(StringComparer.OrdinalIgnoreCase);

	private readonly List<AITool> _aiTools = new();
	private readonly IServiceScopeFactory _scopeFactory;

	public ToolRegistry(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
		RegisterDefaultTools();
	}

	private WebTools CreateWebTools()
	{
		var scope = _scopeFactory.CreateScope();
		return scope.ServiceProvider.GetRequiredService<WebTools>();
	}

	private void RegisterDefaultTools()
	{
		// Create one instance just for AIFunctionFactory metadata registration
		// (It only reads method signatures, doesn't execute)
		using var scope = _scopeFactory.CreateScope();
		var webTools = scope.ServiceProvider.GetRequiredService<WebTools>();

		// Register AI function metadata
		_aiTools.Add(AIFunctionFactory.Create(
			webTools.IngestUrlAsync,
			name: "ingest_url",
			description: "Capture the text content of any website"
		));

		_aiTools.Add(AIFunctionFactory.Create(
			webTools.WebSearchAsync,
			name: "web_search",
			description: "Search the internet for real-time information and ingest results"
		));

		_aiTools.Add(AIFunctionFactory.Create(
			NoToolAsync,
			name: "no_tool",
			description: "Select when no other tool applies"
		));

		_aiTools.Add(AIFunctionFactory.Create(
			GetWeatherFn,
			name: "GetWeather",
			description: "Get current weather"));

		_aiTools.Add(AIFunctionFactory.Create(
			MathAddFn,
			name: "MathAdd",
			description: "Add two integers"));

		// Register execution handlers (these create fresh scoped instances)
		Register("ingest_url", async args =>
		{
			var url = args.TryGetValue("url", out var u) ? u?.ToString() ?? "" : "";
			var wsId = args.TryGetValue("workspaceId", out var w) ? w?.ToString() ?? "" : "";

			var wt = CreateWebTools();
			return await wt.IngestUrlAsync(url, wsId);
		});

		Register("web_search", async args =>
		{
			var query = args.TryGetValue("query", out var q) ? q?.ToString() ?? "" : "";
			var fullMsg = args.TryGetValue("originalMessage", out var m) ? m?.ToString() ?? "" : "";

			var wt = CreateWebTools();
			return await wt.WebSearchAsync(query, fullMsg);
		});

		Register("GetWeather", async args =>
		{
			var city = args.TryGetValue("city", out var v) ? v?.ToString() ?? "unknown" : "unknown";
			return $"Weather for {city}: -2°C, light snow (simulated)";
		});

		Register("MathAdd", async args =>
		{
			int a = Convert.ToInt32(args.TryGetValue("a", out var va) ? va : 0);
			int b = Convert.ToInt32(args.TryGetValue("b", out var vb) ? vb : 0);
			return (a + b).ToString();
		});

		Register("no_tool", async _ => "");
	}

	private static string GetWeatherFn(string city) => $"Weather for {city}.";
	private static string MathAddFn(int a, int b) => (a + b).ToString();
	private static Task<string?> NoToolAsync() => Task.FromResult<string?>(null);

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