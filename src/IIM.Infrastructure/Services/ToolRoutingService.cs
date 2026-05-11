using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using OllamaSharp.Models.Chat;

namespace IIM.Infrastructure.Services;

public sealed class ToolRoutingService : IToolRoutingService
{
	private readonly IAIAgentFactory _agentFactory;
	private readonly IToolRegistry _toolRegistry;

	public ToolRoutingService(
		IAIAgentFactory agentFactory,
		IToolRegistry toolRegistry)
	{
		_agentFactory = agentFactory;
		_toolRegistry = toolRegistry;
	}

	// ===========================================================
	// BACKWARD-COMPAT ENTRY POINT
	// Web search is DISABLED by default
	// ===========================================================
	public Task<ToolDecision> DecideAsync(
		string userInput,
		CancellationToken ct = default)
	{
		return DecideAsync(userInput, allowWebSearch: false, ct);
	}

	// ===========================================================
	// GATED ENTRY POINT
	// ===========================================================
	public async Task<ToolDecision> DecideAsync(
		string userInput,
		bool allowWebSearch,
		CancellationToken ct = default)
	{
		var client = await _agentFactory.GetFunctionClientAsync();
		if (client == null)
		{
			return new ToolDecision(
				ShouldCallTool: false,
				ToolName: null,
				Arguments: null,
				Confidence: "no-client");
		}

		// -------------------------------------------------------
		// TOOL SET — HARD GATE web_search
		// -------------------------------------------------------
		var tools = _toolRegistry
			.GetAIFunctions()
			.Where(t =>
				allowWebSearch ||
				!string.Equals(t.Name, "web_search", StringComparison.OrdinalIgnoreCase))
			.ToList();

		var options = new ChatOptions
		{
			Tools = tools,
			ToolMode = ChatToolMode.Auto,
			Temperature = 0.0f,
			TopP = 1.0f,
			MaxOutputTokens = 128
		};

		var response = await client.GetResponseAsync(
			new[]
			{
				new ChatMessage(
					Microsoft.Extensions.AI.ChatRole.System,
					BuildRouterPrompt(allowWebSearch)),
				new ChatMessage(
					Microsoft.Extensions.AI.ChatRole.User,
					userInput)
			},
			options,
			ct);

		// -------------------------------------------------------
		// PARSE FUNCTION CALL
		// -------------------------------------------------------
		foreach (var message in response.Messages)
		{
			foreach (var content in message.Contents)
			{
				if (content is not FunctionCallContent fn)
					continue;

				// Explicit no_tool
				if (string.Equals(fn.Name, "no_tool", StringComparison.OrdinalIgnoreCase))
				{
					return new ToolDecision(
						ShouldCallTool: false,
						ToolName: fn.Name,
						Arguments: JsonSerializer.SerializeToElement(fn.Arguments),
						Confidence: "high");
				}

				// Final safety net: web search forbidden
				if (!allowWebSearch &&
					string.Equals(fn.Name, "web_search", StringComparison.OrdinalIgnoreCase))
				{
					return new ToolDecision(
						ShouldCallTool: false,
						ToolName: null,
						Arguments: null,
						Confidence: "web-disabled");
				}

				return new ToolDecision(
					ShouldCallTool: true,
					ToolName: fn.Name,
					Arguments: JsonSerializer.SerializeToElement(fn.Arguments),
					Confidence: "high");
			}
		}

		return new ToolDecision(
			ShouldCallTool: false,
			ToolName: null,
			Arguments: null,
			Confidence: "model-declined");
	}

	// ===========================================================
	// PROMPT BUILDER (keeps model honest)
	// ===========================================================
	private static string BuildRouterPrompt(bool allowWebSearch)
	{
		if (!allowWebSearch)
		{
			return
@"You are a STRICT TOOL ROUTER.

WEB SEARCH IS DISABLED.

Rules:
- You MUST NOT select web_search.
- Select ingest_url ONLY if the user provides a URL.
- Otherwise select no_tool.

OUTPUT: Only a function call. No natural language.";
		}

		return
@"You are a STRICT TOOL ROUTER. Select the appropriate tool based on the query.

TOOL SELECTION RULES:

1. SELECT ""web_search"" when:
   - Query asks about current events, news, or recent happenings
   - Query mentions specific dates (""today"", ""latest"", ""recent"")
   - Query asks about current status, prices, roles, or outcomes
   - You are unsure or the topic may have changed

2. SELECT ""ingest_url"" when:
   - Query contains a URL (http:// or https://)
   - Query asks to summarize, read, or analyze a link

3. SELECT ""no_tool"" when:
   - Query is conversational or timeless
   - Query asks for definitions, explanations, or creative output

CRITICAL: When in doubt, SELECT ""web_search"".

OUTPUT: Only a function call. No natural language.";
	}
}
