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
	/// <summary>
	/// Returns the current chat agent, initializing it if needed.
	/// Always async and thread-safe.
	/// </summary>
	Task<AIAgent> GetChatAgentAsync();

	/// <summary>
	/// Returns the current reasoning agent, initializing it if needed.
	/// Always async and thread-safe.
	/// </summary>
	Task<AIAgent> GetReasoningAgentAsync();

	/// <summary>
	/// Forces a full rebuild of all agents the next time they are requested.
	/// </summary>
	void Invalidate();

	/// <summary>
	/// Explicitly reloads the selected model and endpoint configuration.
	/// Safe to call even if agents already exist.
	/// </summary>
	Task ReloadModelsAsync();

	/// <summary>
	/// Returns the model ID of the currently loaded chat model.
	/// </summary>
	string CurrentChatModel { get; }

	/// <summary>
	/// Returns the model ID of the currently loaded reasoning model.
	/// </summary>
	string CurrentReasoningModel { get; }
}
