using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;


namespace IIM.Shared.Interfaces;

/// <summary>
/// Factory responsible for constructing AI agents on demand.
/// Agents are stateful and must not be created eagerly in DI.
/// </summary>
public interface IAIAgentFactory
{
	Task<AIAgent> GetChatAgentAsync();
	Task<AIAgent> GetReasoningAgentAsync();
	Task<IChatClient> GetChatClientAsync(); 
	Task<IChatClient?> GetReasoningClientAsync();  

	Task<IChatClient?> GetFunctionClientAsync();
	string CurrentChatModel { get; }
	string CurrentReasoningModel { get; }
	void Invalidate();
	Task ReloadModelsAsync();
}
