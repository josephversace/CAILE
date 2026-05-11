using IIM.Infrastructure.Ollama;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using IIM.Shared.Models.Configuration;
using IIM.Shared.Models.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace IIM.Api.Services;

public sealed class AIAgentFactory : IAIAgentFactory, IDisposable
{
	private readonly IServiceProvider _services;
	private readonly IToolRegistry _tools;
	private readonly ILogger<AIAgentFactory> _logger;

	private readonly SemaphoreSlim _initLock = new(1, 1);

	private AIAgent? _chatAgent;
	private AIAgent? _reasoningAgent;
	private IChatClient? _chatClient;
	private IChatClient? _reasoningClient;
	private IChatClient? _functionClient;

	private string _chatModel = "";
	private string _reasoningModel = "";

	private bool _initialized;

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
		_functionClient = null;
		_initialized = false;
	}

	public async Task<AIAgent> GetChatAgentAsync()
	{
		await EnsureInitializedAsync();
		return _chatAgent!;
	}

	public async Task<AIAgent> GetReasoningAgentAsync()
	{
		await EnsureInitializedAsync();
		return _reasoningAgent ?? _chatAgent!;
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

	public async Task<IChatClient?> GetFunctionClientAsync()
	{
		await EnsureInitializedAsync();
		return _functionClient;
	}

	public async Task<AIAgent> GetChatAgentAsync(AgentExecutionContext? context)
	{
		// Fast path: no override → existing cached behavior
		var modelId = context?.ModelOverrides?.Primary;
		if (string.IsNullOrWhiteSpace(modelId))
			return await GetChatAgentAsync();

		using var scope = _services.CreateScope();

		var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
		var modelSvc = scope.ServiceProvider.GetRequiredService<IModelService>();
		var promptSnapshots = scope.ServiceProvider.GetRequiredService<IPromptSnapshotProvider>();
		var promptResolver = scope.ServiceProvider.GetRequiredService<PromptResolver>();
		var configSvc = scope.ServiceProvider.GetRequiredService<IModelConfigurationService>();

		// ✅ DO NOT resolve via "configured models" — just synthesize
		var overrideModel = new ActiveModelConfig
		{
			ModelId = modelId!,
			ProviderOverride = null,
			Defaults = null,
			ExplicitPrompt = null,
			PromptOverrideKey = null
		};

		var provider = await resolver.GetProviderAsync(overrideModel);
		var defaults = await resolver.GetInferenceDefaultsAsync(overrideModel);

		await modelSvc.LoadModelForSlotAsync(overrideModel.ModelId, "primary");

		var cfg = await configSvc.GetConfigurationAsync();
		var snapshot = await promptSnapshots.GetSnapshotAsync();

		// ✅ Keep the PRIMARY prompt settings (only model changes)
		var prompt = promptResolver.Resolve(
			snapshot,
			cfg.Active.Primary.ExplicitPrompt,
			cfg.Active.Primary.PromptOverrideKey,
			"chat.default");

		_logger.LogInformation("Using PRIMARY model override: {ModelId}", overrideModel.ModelId);

		return CreateAgent(
			CreateChatClient(provider.Endpoint!, overrideModel.ModelId),
			"ChatAssistant",
			prompt.Content,
			defaults);
	}


	public async Task<AIAgent> GetReasoningAgentAsync(AgentExecutionContext? context)
	{
		var modelId = context?.ModelOverrides?.Secondary;
		if (string.IsNullOrWhiteSpace(modelId))
			return await GetReasoningAgentAsync();

		using var scope = _services.CreateScope();

		var resolver = scope.ServiceProvider.GetRequiredService<IModelResolver>();
		var modelSvc = scope.ServiceProvider.GetRequiredService<IModelService>();
		var promptSnapshots = scope.ServiceProvider.GetRequiredService<IPromptSnapshotProvider>();
		var promptResolver = scope.ServiceProvider.GetRequiredService<PromptResolver>();
		var configSvc = scope.ServiceProvider.GetRequiredService<IModelConfigurationService>();

		var overrideModel = new ActiveModelConfig
		{
			ModelId = modelId!,
			ProviderOverride = null,
			Defaults = null,
			ExplicitPrompt = null,
			PromptOverrideKey = null
		};

		var provider = await resolver.GetProviderAsync(overrideModel);
		var defaults = await resolver.GetInferenceDefaultsAsync(overrideModel);

		await modelSvc.LoadModelForSlotAsync(overrideModel.ModelId, "secondary");

		var cfg = await configSvc.GetConfigurationAsync();
		var snapshot = await promptSnapshots.GetSnapshotAsync();

		// ✅ Keep SECONDARY prompt settings (only model changes)
		var prompt = promptResolver.Resolve(
			snapshot,
			cfg.Active.Secondary?.ExplicitPrompt,
			cfg.Active.Secondary?.PromptOverrideKey,
			"reasoning.default");

		_logger.LogInformation("Using SECONDARY model override: {ModelId}", overrideModel.ModelId);

		return CreateAgent(
			CreateChatClient(provider.Endpoint!, overrideModel.ModelId),
			"ReasoningAssistant",
			prompt.Content,
			defaults);
	}




	// ===========================================================
	// INITIALIZATION
	// ===========================================================
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
			var configSvc = scope.ServiceProvider.GetRequiredService<IModelConfigurationService>();
			var _promptSnapshots = scope.ServiceProvider.GetRequiredService<IPromptSnapshotProvider>();
			var _promptResolver = scope.ServiceProvider.GetRequiredService<PromptResolver>();

			await modelSvc.EnsureInitializedAsync();

			var cfg = await configSvc.GetConfigurationAsync();
			var promptSnapshot = await _promptSnapshots.GetSnapshotAsync();

			// ===================================================
			// PRIMARY (CHAT)
			// ===================================================
			var primaryModel = await resolver.GetPrimaryModelAsync();



			var primaryProvider = await resolver.GetProviderAsync(primaryModel);
			var primaryDefaults = await resolver.GetInferenceDefaultsAsync(primaryModel);

			_chatModel = primaryModel.ModelId;

			await modelSvc.LoadModelForSlotAsync(_chatModel, "primary");

			var primaryPrompt = _promptResolver.Resolve(
				promptSnapshot,
				cfg.Active.Primary.ExplicitPrompt,
				cfg.Active.Primary.PromptOverrideKey,
				"chat.default"
			);

			_chatClient = CreateChatClient(primaryProvider.Endpoint!, _chatModel);
			_chatAgent = CreateAgent(
				_chatClient,
				"ChatAssistant",
				primaryPrompt.Content,
				primaryDefaults
			);

			// ===================================================
			// SECONDARY (REASONING)
			// ===================================================
			var secondaryModel = await resolver.GetSecondaryModelAsync();

			if (secondaryModel != null)
			{
				var secondaryProvider = await resolver.GetProviderAsync(secondaryModel);
				var secondaryDefaults = await resolver.GetInferenceDefaultsAsync(secondaryModel);

				_reasoningModel = secondaryModel.ModelId;

				await modelSvc.LoadModelForSlotAsync(_reasoningModel, "secondary");

				var secondaryPrompt = _promptResolver.Resolve(
					promptSnapshot,
					cfg.Active.Secondary!.ExplicitPrompt,
					cfg.Active.Secondary.PromptOverrideKey,
					"reasoning.default"
				);

				_reasoningClient = CreateChatClient(
					secondaryProvider.Endpoint!,
					_reasoningModel);

				_reasoningAgent = CreateAgent(
					_reasoningClient,
					"ReasoningAssistant",
					secondaryPrompt.Content,
					secondaryDefaults
				);
			}

			// ===================================================
			// FUNCTION CALLING (OPTIONAL)
			// ===================================================
			var functionModel = await resolver.GetFunctionCallingModelAsync();

			if (functionModel != null)
			{
				var provider = await resolver.GetProviderAsync(functionModel);

				_functionClient = CreateChatClient(
					provider.Endpoint!,
					functionModel.ModelId);
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

	// ===========================================================
	// HELPERS
	// ===========================================================
	private static IChatClient CreateChatClient(string endpoint, string model)
	{
		var baseEndpoint = endpoint.Replace("/v1", "");
		return new OllamaApiClient(new Uri(baseEndpoint))
		{
			SelectedModel = model
		};
	}

	private AIAgent CreateAgent(
		IChatClient chatClient,
		string name,
		string instructions,
		InferenceDefaults defaults)
	{
		return chatClient.CreateAIAgent(new ChatClientAgentOptions
		{
			Name = name,
			Description = "AG-UI Agent",
			ChatOptions = new ChatOptions
			{
				Instructions = instructions,
				MaxOutputTokens = defaults.MaxTokens,
				Temperature = (float)defaults.Temperature,
				TopP = (float)defaults.TopP
			}
		});
	}

	public async Task ReloadModelsAsync()
	{
		await _initLock.WaitAsync();
		try
		{
			_logger.LogInformation("Reloading AI agents due to configuration change.");

			// Tear down all cached state
			_chatAgent = null;
			_reasoningAgent = null;
			_chatClient = null;
			_reasoningClient = null;
			_functionClient = null;

			_chatModel = "";
			_reasoningModel = "";

			_initialized = false;
		}
		finally
		{
			_initLock.Release();
		}

		// Force re-init on next access
		await EnsureInitializedAsync();
	}


	public void Dispose()
	{
		_initLock.Dispose();
	}
}
