using GraphRag.LanguageModels;
using Microsoft.Extensions.AI;
using IIM.Shared.Interfaces;

public sealed class AgentGraphRagChatClient
	: IGraphRagChatClient
{
	private readonly IAIAgentFactory _agentFactory;

	public AgentGraphRagChatClient(IAIAgentFactory agentFactory)
	{
		_agentFactory = agentFactory;
	}

	public async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		// IMPORTANT: this must return something that implements IChatClient
		var chatClient = await _agentFactory.GetChatClientAsync();

		options ??= new ChatOptions();
		options.MaxOutputTokens ??= 8192;



		// Delegate directly — no transformation, no guessing
		return await chatClient.GetResponseAsync(
			messages,
			options,
			cancellationToken);
	}
}
