using System.Text;
using System.Text.Json;
using IIM.Infrastructure.Ollama;
using IIM.Shared.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace IIM.Api.Services;

public class AIAgentFactory : IAIAgentFactory, IDisposable
{
	private readonly IServiceProvider _services;
	private readonly IToolRegistry _tools;
	private readonly ILogger<AIAgentFactory> _logger;

	private readonly SemaphoreSlim _initLock = new(1, 1);

	private AIAgent? _chatAgent;
	private AIAgent? _reasoningAgent;
	private IChatClient? _chatClient;
	private IChatClient? _reasoningClient;

	private string _chatModel = "";
	private string _reasoningModel = "";
	private string _endpoint = "";

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
		_chatClient = null;
		_reasoningClient = null;
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
			throw new InvalidOperationException("No reasoning model is configured.");
		return _reasoningAgent;
	}

	public async Task<IChatClient> GetChatClientAsync()
	{
		await EnsureInitializedAsync();
		return _chatClient!;
	}

	public async Task<IChatClient?> GetReasoningClientAsync()
	{
		await EnsureInitializedAsync();
		return _reasoningClient;
	}

	private bool _initialized;

	private async Task EnsureInitializedAsync()
	{
		if (_initialized)
			return;

		await _initLock.WaitAsync();
		try
		{
			if (_initialized)
				return;

			_logger.LogInformation("Initializing AI agents...");

			using var scope = _services.CreateScope();
			var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
			var modelSvc = scope.ServiceProvider.GetRequiredService<IModelService>();

			await modelSvc.EnsureInitializedAsync();
			_endpoint = modelSvc.InferenceEndpoint;

			// Primary model (required)
			var primary = await resolver.GetPrimaryModelAsync();
			_chatModel = primary.ModelId;
			_logger.LogInformation("Initializing chat model: {Model}", _chatModel);

			await modelSvc.LoadModelForSlotAsync(_chatModel, "primary");
			_chatClient = CreateChatClient(_chatModel);
			_chatAgent = CreateAgent(
				_chatClient,
				"ChatAssistant",
				primary.SystemPrompt ?? GetChatInstructions(),
				false);

			// Secondary model (optional)
			var secondary = await resolver.GetSecondaryModelAsync();
			if (secondary != null && !string.IsNullOrWhiteSpace(secondary.ModelId))
			{
				_reasoningModel = secondary.ModelId;
				_logger.LogInformation("Initializing reasoning model: {Model}", _reasoningModel);

				await modelSvc.LoadModelForSlotAsync(_reasoningModel, "secondary");
				_reasoningClient = CreateChatClient(_reasoningModel);
				_reasoningAgent = CreateAgent(
					_reasoningClient,
					"ReasoningAssistant",
					secondary.SystemPrompt ?? GetReasoningInstructions(),
					true);
			}
			else
			{
				_reasoningModel = "";
				_reasoningClient = null;
				_reasoningAgent = null;
			}

			_initialized = true;

			_logger.LogInformation(
				"AI agents initialized (chat={Chat}, reasoning={Reasoning})",
				_chatModel,
				string.IsNullOrEmpty(_reasoningModel) ? "none" : _reasoningModel);
		}
		finally
		{
			_initLock.Release();
		}
	}

	public async Task ReloadModelsAsync()
	{
		await _initLock.WaitAsync();
		try
		{
			using var scope = _services.CreateScope();
			var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
			var modelSvc = scope.ServiceProvider.GetRequiredService<IModelService>();

			await modelSvc.EnsureInitializedAsync();
			_endpoint = modelSvc.InferenceEndpoint;

			// Check if chat model changed
			var primary = await resolver.GetPrimaryModelAsync();
			var newChatModel = primary.ModelId;

			if (!string.Equals(_chatModel, newChatModel, StringComparison.OrdinalIgnoreCase)
				&& !string.IsNullOrEmpty(newChatModel))
			{
				_logger.LogInformation("Chat model changed: {Old} → {New}", _chatModel, newChatModel);

				await modelSvc.LoadModelForSlotAsync(newChatModel, "primary");
				_chatModel = newChatModel;
				_chatClient = CreateChatClient(_chatModel);
				_chatAgent = CreateAgent(
					_chatClient,
					"ChatAssistant",
					primary.SystemPrompt ?? GetChatInstructions(),
					false);
			}

			// Check if reasoning model changed
			var secondary = await resolver.GetSecondaryModelAsync();
			var newReasoningModel = secondary?.ModelId ?? "";

			if (!string.Equals(_reasoningModel, newReasoningModel, StringComparison.OrdinalIgnoreCase))
			{
				_logger.LogInformation("Reasoning model changed: {Old} → {New}", _reasoningModel, newReasoningModel);

				if (!string.IsNullOrEmpty(newReasoningModel))
				{
					await modelSvc.LoadModelForSlotAsync(newReasoningModel, "secondary");
					_reasoningModel = newReasoningModel;
					_reasoningClient = CreateChatClient(_reasoningModel);
					_reasoningAgent = CreateAgent(
						_reasoningClient,
						"ReasoningAssistant",
						secondary!.SystemPrompt ?? GetReasoningInstructions(),
						true);
				}
				else
				{
					await modelSvc.UnloadSlotAsync("secondary");
					_reasoningModel = "";
					_reasoningClient = null;
					_reasoningAgent = null;
				}
			}
		}
		finally
		{
			_initLock.Release();
		}
	}

	private IChatClient CreateChatClient(string model)
	{
		// OllamaSharp implements IChatClient directly
		// Remove /v1 suffix if present since OllamaSharp uses native API
		var baseEndpoint = _endpoint.Replace("/v1", "");
		return new OllamaApiClient(new Uri(baseEndpoint))
		{
			SelectedModel = model
		};
	}

	private AIAgent CreateAgent(IChatClient chatClient, string name, string instructions, bool enableTools)
	{
		if (enableTools)
		{
			var tools = _tools.GetAIFunctions();

			return chatClient.CreateAIAgent(new ChatClientAgentOptions
			{
				Name = name,
				Instructions = instructions,
				Description = "AG-UI Agent",
				ChatOptions = new ChatOptions
				{
					MaxOutputTokens = 8192,
					Temperature = 0.7f,
					TopP = 0.9f,
					Tools = tools,
					ToolMode = ChatToolMode.Auto
				}
			});
		}
		else
		{
			return chatClient.CreateAIAgent(new ChatClientAgentOptions
			{
				Name = name,
				Instructions = instructions,
				Description = "AG-UI Agent",
				ChatOptions = new ChatOptions
				{
					MaxOutputTokens = 8192,
					Temperature = 0.7f,
					TopP = 0.9f
				}
			});
		}
	}

	private string GetChatInstructions() => @"
### ROLE
You are a professional investigative analyst assistant.

### GENERAL BEHAVIOR
- When no <context> is provided, answer helpfully using your general knowledge.
- When <context> IS provided, ground your answers strictly in that evidence.

### WHEN CONTEXT IS PROVIDED
- Only use information from <context> tags
- If insufficient evidence, say so clearly
- Never fabricate entities, dates, or conclusions
- Quote or paraphrase only what exists in context

### OUTPUT STYLE
- Be concise and direct (1-3 sentences typical)
- No chain-of-thought explanations
- Plain text, no LaTeX or markdown tables
";

	private string GetReasoningInstructions() => @"
You are an investigative analyst chat assistant.

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
";

	public void Dispose()
	{
		_initLock.Dispose();
	}
}