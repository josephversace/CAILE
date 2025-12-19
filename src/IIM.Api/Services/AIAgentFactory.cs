using System.ClientModel;
using System.ClientModel.Primitives;
using IIM.Api.Services;
using IIM.Shared.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Microsoft.Agents.AI;

namespace IIM.Api.Services;

public class AIAgentFactory : IAIAgentFactory, IDisposable
{
	private readonly IServiceProvider _services;
	private readonly IToolRegistry _tools;
	private readonly ILogger<AIAgentFactory> _logger;

	private readonly SemaphoreSlim _initLock = new(1, 1);

	private AIAgent? _chatAgent;
	private AIAgent? _reasoningAgent;
	private IChatClient? _chatClient;      // NEW
	private IChatClient? _reasoningClient; // NEW

	private string _chatModel = "";
	private string _reasoningModel = "";
	private string _endpoint = "";
	private string? _lastEndpoint;
	private bool _reasoningModelLoaded = false;

	public string CurrentChatModel => _chatModel;
	public string CurrentReasoningModel => _reasoningModel;

	public AIAgentFactory(
		IServiceProvider services,
		IToolRegistry tools,
		ILogger<AIAgentFactory> logger)
	{
		_services = services;
		_tools = tools;
		_logger = logger;
	}

	public void Invalidate()
	{
		_chatAgent = null;
		_reasoningAgent = null;
		_chatClient = null;      // NEW
		_reasoningClient = null; // NEW
	}

	public async Task<AIAgent> GetChatAgentAsync()
	{
		await EnsureInitializedAsync();
		return _chatAgent!;
	}

	public async Task<AIAgent> GetReasoningAgentAsync()
	{
		await EnsureInitializedAsync();
		if (_reasoningAgent == null)
			throw new InvalidOperationException("No reasoning model is configured in the current template.");
		return _reasoningAgent;
	}

	// NEW METHOD
	public async Task<IChatClient> GetChatClientAsync()
	{
		await EnsureInitializedAsync();
		return _chatClient!;
	}

	// NEW METHOD (optional, if you need reasoning client too)
	public async Task<IChatClient?> GetReasoningClientAsync()
	{
		await EnsureInitializedAsync();
		return _reasoningClient;
	}

	private async Task EnsureInitializedAsync()
	{
		if (_chatAgent != null && _chatClient != null &&
			(_reasoningModelLoaded == false || _reasoningAgent != null))
			return;

		await _initLock.WaitAsync();
		try
		{
			if (_chatAgent == null || _chatClient == null ||
				(_reasoningModelLoaded && _reasoningAgent == null))
			{
				await ReloadModelsInternalAsync();
			}
		}
		finally
		{
			_initLock.Release();
		}
	}

	private async Task ReloadModelsInternalAsync()
	{
		using var scope = _services.CreateScope();
		var resolver = scope.ServiceProvider.GetRequiredService<IModelTemplateResolver>();
		var foundry = _services.GetRequiredService<IFoundryEndpointProvider>();

		var newEndpoint = foundry.GetBaseUrl();

		if (_lastEndpoint != null && _lastEndpoint != newEndpoint)
		{
			_logger.LogWarning("Foundry endpoint changed: {Old} → {New}",
				_lastEndpoint, newEndpoint);
		}

		_lastEndpoint = newEndpoint;
		_endpoint = newEndpoint + "/v1";

		var template = await resolver.GetActiveTemplateAsync();

		_chatModel = template.Models.Chat.FoundryModelId;

		// CHANGED: Create client first, store it, then create agent
		_chatClient = CreateChatClient(_chatModel);
		_chatAgent = CreateAgent(_chatClient, "ChatAssistant", GetChatInstructions());

		bool hasReasoning = template.Models.Reasoning?.FoundryModelId is { Length: > 0 };

		if (hasReasoning)
		{
			_reasoningModel = template.Models.Reasoning!.FoundryModelId;
			_reasoningClient = CreateChatClient(_reasoningModel);
			_reasoningAgent = CreateAgent(_reasoningClient, "ReasoningAssistant", GetReasoningInstructions());
			_reasoningModelLoaded = true;
		}
		else
		{
			_reasoningClient = null;
			_reasoningAgent = null;
			_reasoningModelLoaded = false;
		}

		_logger.LogInformation("Agents rebuilt. Endpoint: {Endpoint}", _endpoint);
	}

	public async Task ReloadModelsAsync()
	{
		await EnsureInitializedAsync();
	}

	// NEW: Separate method to create the raw client
	private IChatClient CreateChatClient(string model)
	{
		return new ChatClient(
			model: model,
			credential: new ApiKeyCredential("local"),
			options: new OpenAIClientOptions
			{
				Endpoint = new Uri(_endpoint),
				Transport = new HttpClientPipelineTransport(
					new HttpClient { Timeout = TimeSpan.FromMinutes(10) })
			}
		).AsIChatClient();
	}

	// CHANGED: Now takes IChatClient instead of model string
	private AIAgent CreateAgent(IChatClient chatClient, string name, string instructions)
	{
		var tools = _tools.GetAIFunctions();

		return chatClient.CreateAIAgent(new ChatClientAgentOptions
		{
			Name = name,
			Instructions = instructions,
			Description = $"AG-UI Agent",
			ChatOptions = new ChatOptions
			{
				MaxOutputTokens = 4096,
				Temperature = 0.7f,
				TopP = 0.9f,
				Tools = tools,
				ToolMode = ChatToolMode.Auto
			}
		});
	}

	private string GetChatInstructions() => """
You are CAILE’s primary chat assistant.

GENERAL RULES:
1. Answer concisely and directly.
2. Do NOT show chain-of-thought. Use short explanations only when needed.
3. For regex: give only the regex + a 1–2 line summary.
4. For math: give the final answer unless asked for steps.
5. Avoid LaTeX unless the user explicitly requests it.

TOOL USE RULES:
1. If you need to use a tool, output ONLY a <tool_call>{...}</tool_call> block.
2. Nothing is allowed before or after the <tool_call> block.
3. Arguments MUST be valid JSON.
4. Never describe or explain the tool call.
5. After receiving tool results, the system will send you a follow-up message.
   At that time, answer normally and DO NOT call another tool unless necessary.

FAILURE MODES TO AVOID:
- Do NOT mix natural language with tool_call JSON.
- Do NOT add emojis, prefixes, suffixes, or other characters around a tool call.
- Do NOT hallucinate tool names.
""";


	private string GetReasoningInstructions() => """
You are CAILE’s deliberate reasoning agent.

REASONING RULES:
1. You MAY show full chain-of-thought when answering directly.
2. Think step-by-step and justify your conclusions.
3. Use clear logic, derivations, and intermediate steps.
4. Use LaTeX when helpful for equations.
5. Provide a short final answer after the reasoning.

TOOL USE RULES:
1. When you decide to use a tool, you MUST output ONLY:
     <tool_call>{ "name": "...", "arguments": { ... } }</tool_call>
2. DO NOT include any chain-of-thought before, during, or after a tool call.
3. Tool call arguments must be valid JSON.
4. After tool results are provided back to you, produce the final reasoning and answer WITHOUT calling another tool unless logically required.

FAILURE MODES TO AVOID:
- Do NOT mix chain-of-thought with tool_call JSON.
- Do NOT emit anything except the <tool_call> block when calling a tool.
- Do NOT wrap JSON in markdown.
- Do NOT output malformed or partial JSON.
""";



	public void Dispose()
	{
		_initLock.Dispose();
	}
}
