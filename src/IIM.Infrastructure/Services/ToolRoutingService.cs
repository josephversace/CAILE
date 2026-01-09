using System.Text.Json;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.Extensions.AI;
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
			@"You are a STRICT TOOL ROUTER. Select the appropriate tool based on the query.

TOOL SELECTION RULES:

1. SELECT ""web_search"" when:
   - Query asks about current events, news, or recent happenings
   - Query mentions specific dates, ""today"", ""yesterday"", ""this week"", ""latest"", ""recent""
   - Query asks about current status (""who is the president"", ""current price"", ""latest score"")
   - Query asks ""why did X happen"" about real-world events
   - Query asks about anything after 2023 (your knowledge cutoff)
   - Query involves people's current roles, positions, or status
   - You are unsure or the topic might have changed

2. SELECT ""ingest_url"" when:
   - Query contains a URL (http:// or https://)
   - Query asks to summarize, read, or analyze a specific link

3. SELECT ""no_tool"" ONLY when:
   - Query is purely conversational (""hello"", ""thanks"")
   - Query asks about timeless facts (""what is photosynthesis"")
   - Query asks about historical events with fixed outcomes (""when was WW2"")
   - Query is about definitions or concepts (""what is a neural network"")
   - Query asks you to translate, write, or create content

CRITICAL: When in doubt, SELECT ""web_search"". It is better to search and confirm than to guess.

OUTPUT: Only a function call. No natural language."
		),
		new ChatMessage(Microsoft.Extensions.AI.ChatRole.User, userInput)
			},
			options,
			ct);

		var tools = string.Join(", ", options.Tools.Select(t => t.Name));

	
		foreach (var message in response.Messages)
		{
			foreach (var content in message.Contents)
			{
				if (content is FunctionCallContent fn)
				{

					if (fn.Name == "no_tool")
					{
						return new ToolDecision(
							ShouldCallTool: false,
							ToolName: fn.Name,
							Arguments: JsonSerializer.SerializeToElement(fn.Arguments),
							Confidence: "high");
					}


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

}