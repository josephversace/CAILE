using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using Microsoft.Extensions.AI;

namespace IIM.Application.Workflows;

/// <summary>
/// Exposes the Risk Analysis Workflow as a tool callable by the chat agent.
/// </summary>
public class RiskAnalysisToolProvider
{
	private readonly IAIAgentFactory _agentFactory;
	private readonly IPolicyRepository _policyRepository;

	public RiskAnalysisToolProvider(
		IAIAgentFactory agentFactory,
		IPolicyRepository policyRepository)
	{
		_agentFactory = agentFactory;
		_policyRepository = policyRepository;
	}

	/// <summary>
	/// Get the tool definition for registering with the chat agent.
	/// </summary>
	public AIFunction GetTool() => AIFunctionFactory.Create(AnalyzeProjectRisksAsync);

	/// <summary>
	/// Analyzes project risks against applicable policies and regulations.
	/// </summary>
	/// <param name="risks">List of risks with their current mitigations</param>
	/// <param name="projectContext">Optional context about the project (industry, data sensitivity, etc.)</param>
	/// <returns>Comprehensive risk analysis with classifications, compliance status, and recommendations</returns>
	[Description("Analyzes project risks against applicable policies and regulations. " +
				 "Returns risk classifications, compliance assessments, and prioritized recommendations.")]
	private async Task<string> AnalyzeProjectRisksAsync(
		[Description("JSON array of risks, each with 'risk' and 'mitigation' properties")]
		string risks,
		[Description("Optional project context for policy filtering (industry, jurisdiction, data classification)")]
		string? projectContext = null,
		CancellationToken cancellationToken = default)
	{
		// Parse the risks from the LLM
		var parsedRisks = JsonSerializer.Deserialize<List<ProjectRisk>>(risks, JsonOptions)
						 ?? new List<ProjectRisk>();

		if (parsedRisks.Count == 0)
		{
			return JsonSerializer.Serialize(new { error = "No risks provided for analysis" });
		}

		// Get relevant policies from your knowledge base
		var policies = await _policyRepository.GetRelevantPoliciesAsync(
			parsedRisks,
			projectContext,
			cancellationToken);

		// Run the workflow
		var workflow = new RiskAnalysisWorkflowRunner(_agentFactory);

		try
		{
			var result = await workflow.RunAsync(parsedRisks, policies, cancellationToken);
			return JsonSerializer.Serialize(result, JsonOptions);
		}
		catch (Exception ex)
		{
			return JsonSerializer.Serialize(new
			{
				error = "Risk analysis failed",
				message = ex.Message
			});
		}
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true
	};
}

/// <summary>
/// Interface for retrieving policies from your knowledge graph/vector store.
/// </summary>
public interface IPolicyRepository
{
	Task<IEnumerable<PolicyReference>> GetRelevantPoliciesAsync(
		IEnumerable<ProjectRisk> risks,
		string? projectContext = null,
		CancellationToken cancellationToken = default);
}