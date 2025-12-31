using System.Runtime.CompilerServices;
using System.Text.Json;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml.Office;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
using NPOI.SS.Formula.Functions;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace IIM.Infrastructure.Services;

public sealed class ToolRoutingService : IToolRoutingService
{
	private readonly IAIAgentFactory _agentFactory;
	private readonly IToolRegistry _toolRegistry;

	public ToolRoutingService(IAIAgentFactory agentFactory, IToolRegistry toolRegistry)
	{
		_agentFactory = agentFactory;
		_toolRegistry = toolRegistry;
	}

	public async Task<ToolDecision> DecideAsync(
	string userInput,
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

		var options = new ChatOptions
		{
			Tools = _toolRegistry.GetAIFunctions(), // REQUIRED
			ToolMode = ChatToolMode.Auto,           // REQUIRED
			Temperature = 0.0f,
			TopP = 1.0f,
			MaxOutputTokens = 128
		};

		var response = await client.GetResponseAsync(
			new[]
			{
			new ChatMessage(
				Microsoft.Extensions.AI.ChatRole.System,
				"You can call software functions using the provided schemas."),
			new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userInput)
			},
			options,
			ct);

		// 🔑 THIS is the only correct way to detect a tool call
		foreach (var message in response.Messages)
		{
			foreach (var content in message.Contents)
			{
				if (content is FunctionCallContent fn)
				{
					return new ToolDecision(
						ShouldCallTool: true,
						ToolName: fn.Name,
						Arguments: JsonSerializer.SerializeToElement(fn.Arguments),
						Confidence: "high");
				}
			}
		}

		return new ToolDecision(
			ShouldCallTool: false,
			ToolName: null,
			Arguments: null,
			Confidence: "model-declined");
	}


	private static ToolDecision NoTool(string reason)
	{
		return new ToolDecision(
			ShouldCallTool: false,
			ToolName: null,
			Arguments: null,
			Confidence: reason);
	}


	// ──────────────────────────────────────────────
	// PRIVATE IMPLEMENTATION DETAILS
	// ──────────────────────────────────────────────
	private static List<ChatMessage> BuildRoutingMessages(string input)
		=> new()
		{
		new(Microsoft.Extensions.AI.ChatRole.System,
@"OUTPUT FORMAT CONTRACT (STRICT):

You are a routing engine, not a chat assistant.

You must output EXACTLY one of the following tokens:

NO_TOOL

OR

CALL:<tool_name>|<json>

Any other output is invalid and will be discarded."),

		new(Microsoft.Extensions.AI.ChatRole.User, input)
		};


	private static ToolDecision ParseToolDecision(string text)
	{
		text = text.Trim();

		if (text.Equals("NO_TOOL", StringComparison.OrdinalIgnoreCase))
		{
			return new ToolDecision(
				ShouldCallTool: false,
				ToolName: null,
				Arguments: null,
				Confidence: "high");
		}

		if (!text.StartsWith("CALL ", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Invalid router response");

		var lines = text.Split('\n', 2);
		if (lines.Length != 2)
			throw new InvalidOperationException("Missing arguments");

		var toolName = lines[0].Substring(5).Trim();

		var args = JsonSerializer.Deserialize<Dictionary<string, object?>>(
			lines[1],
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
			?? throw new InvalidOperationException("Invalid arguments JSON");

		return new ToolDecision(
			ShouldCallTool: true,
			ToolName: toolName,
			Arguments: JsonSerializer.SerializeToElement(args),
			Confidence: "high");
	}
}
