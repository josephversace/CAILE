using System.Threading.Tasks;
using IIM.Shared.Models.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace IIM.Shared.Interfaces;

/// <summary>
/// Factory responsible for constructing and managing runtime AI agents.
/// Agents are stateful and must not be created eagerly in DI.
/// </summary>
public interface IAIAgentFactory
{
	// ===========================================================
	// AGENTS (HIGH-LEVEL)
	// ===========================================================

	Task<AIAgent> GetChatAgentAsync();
	Task<AIAgent> GetReasoningAgentAsync();

	// ===========================================================
	// CLIENTS (LOW-LEVEL / INFRA)
	// ===========================================================

	Task<IChatClient> GetChatClientAsync();
	Task<IChatClient?> GetReasoningClientAsync();

	/// <summary>
	/// Returns a client capable of function calling, if configured.
	/// May be null if no such capability exists.
	/// </summary>
	Task<IChatClient?> GetFunctionClientAsync();


	Task<AIAgent> GetChatAgentAsync(AgentExecutionContext? context);

	Task<AIAgent> GetReasoningAgentAsync(AgentExecutionContext? context);


	// ===========================================================
	// LIFECYCLE / STATE
	// ===========================================================

	/// <summary>
	/// Invalidates all cached agents and clients.
	/// Forces re-resolution on next access.
	/// </summary>
	void Invalidate();

	/// <summary>
	/// Reloads models and agents based on updated configuration.
	/// </summary>
	Task ReloadModelsAsync();

	// ===========================================================
	// DIAGNOSTICS (NON-CONTRACTUAL)
	// ===========================================================

	/// <summary>
	/// Currently resolved chat model ID (diagnostic only).
	/// </summary>
	string? CurrentChatModel { get; }

	/// <summary>
	/// Currently resolved reasoning model ID (diagnostic only).
	/// </summary>
	string? CurrentReasoningModel { get; }
}
