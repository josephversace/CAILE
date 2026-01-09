using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using IIM.Shared.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace IIM.Application.Workflows;

/// <summary>
/// Sequential Risk Analysis Workflow using Microsoft Agent Framework.
/// 
/// Pipeline:
/// 1. RiskClassifier → Classifies risks by category, severity, likelihood
/// 2. PolicyMapper → Maps risks to applicable policies (uses context policies)
/// 3. MitigationAssessor → Evaluates mitigation effectiveness and compliance
/// 4. RecommendationGenerator → Creates prioritized recommendations
/// </summary>
public class RiskAnalysisWorkflowRunner
{
	private readonly IAIAgentFactory _agentFactory;

	public RiskAnalysisWorkflowRunner(IAIAgentFactory agentFactory)
	{
		_agentFactory = agentFactory;
	}

	/// <summary>
	/// Execute the risk analysis workflow.
	/// </summary>
	public async Task<RiskAnalysisResult> RunAsync(
		IEnumerable<ProjectRisk> risks,
		IEnumerable<PolicyReference> policies,
		CancellationToken cancellationToken = default)
	{
		// Get chat client from factory
		var chatClient = await _agentFactory.GetChatClientAsync();

		// Create the specialized agents
		var classifierAgent = CreateAgent(chatClient, "RiskClassifier", ClassifierPrompt);
		var policyMapperAgent = CreateAgent(chatClient, "PolicyMapper", PolicyMapperPrompt);
		var mitigationAgent = CreateAgent(chatClient, "MitigationAssessor", MitigationAssessorPrompt);
		var recommendationAgent = CreateAgent(chatClient, "RecommendationGenerator", RecommendationPrompt);

		// Build sequential workflow
		var workflow = new WorkflowBuilder(classifierAgent)
			.AddEdge(classifierAgent, policyMapperAgent)
			.AddEdge(policyMapperAgent, mitigationAgent)
			.AddEdge(mitigationAgent, recommendationAgent)
			.WithOutputFrom(recommendationAgent)
			.Build();

		// Prepare input with risks and policies
		var inputMessage = new ChatMessage(ChatRole.User, FormatInput(risks, policies));

		// Execute workflow with streaming
		await using var run = await InProcessExecution.StreamAsync(workflow, inputMessage, null, cancellationToken);

		// Send turn token to start processing
		await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

		string? finalOutput = null;

		await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
		{
			switch (evt)
			{
				case AgentRunUpdateEvent update:
					// Stream progress - hook this to your UI if needed
					OnProgress?.Invoke(update.ExecutorId, update.Data?.ToString() ?? "");
					break;

				case ExecutorCompletedEvent completed:
					OnStepCompleted?.Invoke(completed.ExecutorId);
					break;

				case WorkflowOutputEvent output:
					finalOutput = output.Data?.ToString();
					break;
			}
		}

		return ParseResult(finalOutput);
	}

	/// <summary>
	/// Execute using the reasoning model for more complex analysis.
	/// </summary>
	public async Task<RiskAnalysisResult> RunWithReasoningAsync(
		IEnumerable<ProjectRisk> risks,
		IEnumerable<PolicyReference> policies,
		CancellationToken cancellationToken = default)
	{
		var reasoningClient = await _agentFactory.GetReasoningClientAsync();

		if (reasoningClient is null)
		{
			// Fall back to chat client if reasoning not available
			return await RunAsync(risks, policies, cancellationToken);
		}

		// Use reasoning model for the complex agents
		var chatClient = await _agentFactory.GetChatClientAsync();

		var classifierAgent = CreateAgent(chatClient, "RiskClassifier", ClassifierPrompt);
		var policyMapperAgent = CreateAgent(reasoningClient, "PolicyMapper", PolicyMapperPrompt);
		var mitigationAgent = CreateAgent(reasoningClient, "MitigationAssessor", MitigationAssessorPrompt);
		var recommendationAgent = CreateAgent(reasoningClient, "RecommendationGenerator", RecommendationPrompt);

		var workflow = new WorkflowBuilder(classifierAgent)
			.AddEdge(classifierAgent, policyMapperAgent)
			.AddEdge(policyMapperAgent, mitigationAgent)
			.AddEdge(mitigationAgent, recommendationAgent)
			.WithOutputFrom(recommendationAgent)
			.Build();

		var inputMessage = new ChatMessage(ChatRole.User, FormatInput(risks, policies));

		await using var run = await InProcessExecution.StreamAsync(workflow, inputMessage, null, cancellationToken);
		await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

		string? finalOutput = null;

		await foreach (var evt in run.WatchStreamAsync().WithCancellation(cancellationToken))
		{
			switch (evt)
			{
				case AgentRunUpdateEvent update:
					OnProgress?.Invoke(update.ExecutorId, update.Data?.ToString() ?? "");
					break;

				case ExecutorCompletedEvent completed:
					OnStepCompleted?.Invoke(completed.ExecutorId);
					break;

				case WorkflowOutputEvent output:
					finalOutput = output.Data?.ToString();
					break;
			}
		}

		return ParseResult(finalOutput);
	}

	#region Events

	/// <summary>
	/// Raised when an agent produces streaming output.
	/// </summary>
	public event Action<string, string>? OnProgress;

	/// <summary>
	/// Raised when an agent completes its step.
	/// </summary>
	public event Action<string>? OnStepCompleted;

	#endregion

	#region Private Methods

	private static ChatClientAgent CreateAgent(IChatClient chatClient, string name, string instructions)
	{
		return new ChatClientAgent(chatClient, name: name, instructions: instructions);
	}

	private static string FormatInput(
		IEnumerable<ProjectRisk> risks,
		IEnumerable<PolicyReference> policies)
	{
		var sb = new StringBuilder();

		sb.AppendLine("# Risk Analysis Request");
		sb.AppendLine();
		sb.AppendLine("## Project Risks:");
		sb.AppendLine();

		var riskList = risks.ToList();
		for (int i = 0; i < riskList.Count; i++)
		{
			sb.AppendLine($"### Risk {i + 1}");
			sb.AppendLine($"**Description:** {riskList[i].Risk}");
			sb.AppendLine($"**Current Mitigation:** {riskList[i].Mitigation}");
			sb.AppendLine();
		}

		sb.AppendLine("## Applicable Policies and Regulations:");
		sb.AppendLine();

		foreach (var policy in policies)
		{
			sb.AppendLine($"### [{policy.Id}] {policy.Name}");
			sb.AppendLine($"**Source:** {policy.Source}");
			sb.AppendLine(policy.Content);
			sb.AppendLine();
		}

		return sb.ToString();
	}

	private static RiskAnalysisResult ParseResult(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new InvalidOperationException("Workflow produced no output");
		}

		// Clean potential markdown fences
		json = json.Trim();
		if (json.StartsWith("```"))
		{
			var lines = json.Split('\n');
			json = string.Join("\n", lines.Skip(1).Take(lines.Length - 2));
		}

		return JsonSerializer.Deserialize<RiskAnalysisResult>(json, JsonOptions)
			   ?? throw new InvalidOperationException("Failed to parse workflow result");
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	#endregion

	#region Agent Prompts

	private const string ClassifierPrompt = """
        You are a senior risk analyst specializing in IT project governance.
        
        Analyze the provided risks and classify each by:
        - Category: Technical, Operational, Financial, Compliance, Security, Resource
        - Severity: Critical (business-stopping), High (major impact), Medium (moderate impact), Low (minor impact)
        - Likelihood: AlmostCertain (>90%), Likely (60-90%), Possible (30-60%), Unlikely (10-30%), Rare (<10%)
        
        Also identify risk interdependencies - which risks could trigger or amplify others.
        
        Pass your complete analysis to the next agent, including the original risk descriptions, 
        mitigations, and the applicable policies context.
        
        Format your output as structured JSON that the next agent can consume.
        """;

	private const string PolicyMapperPrompt = """
        You are a compliance specialist who maps risks to applicable policies and regulations.
        
        Using the risk classifications from the previous agent and the policies provided in the context:
        
        1. For each risk, identify which policies are implicated
        2. Cite specific sections by their reference ID (e.g., "NIST-SC-7", "PIPEDA-4.7")
        3. Note the obligation type: Mandatory, Recommended, Optional, Conditional
        4. Identify any mandatory controls or reporting requirements
        
        CRITICAL RULES:
        - ONLY cite policies explicitly provided in the context
        - Use exact policy IDs from the context
        - Do NOT fabricate or assume sources not provided
        
        Pass your complete analysis including all prior context to the next agent.
        """;

	private const string MitigationAssessorPrompt = """
        You are a risk mitigation expert evaluating mitigation effectiveness.
        
        Using the risk classifications and policy mappings from previous agents, assess each mitigation:
        
        1. Effectiveness: Strong, Adequate, Weak, Insufficient
        2. Gaps: What does the mitigation fail to address?
        3. Dependencies: What does the mitigation rely on?
        4. Compliance Status: Full, Partial, NonCompliant, NotAssessed
        5. Compliance Gaps: Which specific policy requirements are not met?
        6. Residual Risk: What exposure remains after mitigation?
        
        Pass your complete analysis to the final agent for recommendations.
        """;

	private const string RecommendationPrompt = """
        You are a senior risk analyst synthesizing findings into actionable recommendations.
        
        Using all prior analysis, produce a final risk assessment as JSON:
        
        {
          "risks": [
            {
              "risk": "original risk text",
              "mitigation": "original mitigation text",
              "classification": {
                "category": "Technical",
                "severity": "High",
                "likelihood": "Possible"
              },
              "citations": ["POLICY-ID-1"],
              "mitigationAssessment": {
                "effectiveness": "Adequate",
                "gaps": ["gap 1"],
                "dependencies": ["dependency 1"],
                "complianceStatus": "Partial",
                "complianceGaps": ["requirement not met"]
              },
              "relatedRisks": ["related risk text"],
              "residualRisk": "Medium - exposure description"
            }
          ],
          "crossCuttingThemes": [
            {
              "theme": "Theme name",
              "affectedRisks": ["risk 1", "risk 2"],
              "citations": ["POLICY-ID"],
              "recommendation": "Action to take"
            }
          ],
          "overallRiskPosture": "Moderate",
          "complianceSummary": {
            "fullCompliance": ["POLICY-1"],
            "partialCompliance": ["POLICY-2"],
            "nonCompliance": [],
            "notAssessed": []
          },
          "prioritizedRecommendations": [
            {
              "priority": 1,
              "action": "Specific action",
              "addressesRisks": ["risk text"],
              "citations": ["POLICY-ID"],
              "effort": "Low|Medium|High",
              "impact": "Low|Medium|High",
              "remediationDeadline": "timeframe based on policy"
            }
          ]
        }
        
        Output ONLY valid JSON. No markdown fences. No explanatory text.
        """;

	#endregion
}

#region Models

public record ProjectRisk(string Risk, string Mitigation);

public record PolicyReference(string Id, string Name, string Source, string Content);

public record RiskAnalysisResult(
	List<AnalyzedRisk> Risks,
	List<CrossCuttingTheme> CrossCuttingThemes,
	string OverallRiskPosture,
	ComplianceSummary ComplianceSummary,
	List<PrioritizedRecommendation> PrioritizedRecommendations
);

public record AnalyzedRisk(
	string Risk,
	string Mitigation,
	RiskClassification Classification,
	List<string> Citations,
	MitigationAssessment MitigationAssessment,
	List<string> RelatedRisks,
	string ResidualRisk
);

public record RiskClassification(
	RiskCategory Category,
	Severity Severity,
	Likelihood Likelihood
);

public record MitigationAssessment(
	Effectiveness Effectiveness,
	List<string> Gaps,
	List<string> Dependencies,
	ComplianceStatus ComplianceStatus,
	List<string> ComplianceGaps
);

public record CrossCuttingTheme(
	string Theme,
	List<string> AffectedRisks,
	List<string> Citations,
	string Recommendation
);

public record ComplianceSummary(
	List<string> FullCompliance,
	List<string> PartialCompliance,
	List<string> NonCompliance,
	List<string> NotAssessed
);

public record PrioritizedRecommendation(
	int Priority,
	string Action,
	List<string> AddressesRisks,
	List<string> Citations,
	string Effort,
	string Impact,
	string? RemediationDeadline = null
);

public enum RiskCategory { Technical, Operational, Financial, Compliance, Security, Resource }
public enum Severity { Critical, High, Medium, Low }
public enum Likelihood { AlmostCertain, Likely, Possible, Unlikely, Rare }
public enum Effectiveness { Strong, Adequate, Weak, Insufficient }
public enum ComplianceStatus { Full, Partial, NonCompliant, NotAssessed }

#endregion