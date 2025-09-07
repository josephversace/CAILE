using System.Collections.Generic;
using IIM.Shared.Models.Core;

namespace IIM.Application.Governance;

// Command to initiate the AI Governance Wizard
public record ProposeGovernanceRulesCommand(string PolicyDocumentText, string OrgChartText);

// DTO for the AI's proposed framework, for UI display and approval
public record ProposedGovernanceFramework
{
    public List<ClassificationTag> ClassificationTags { get; init; } = new();
    public List<StorageTier> StorageTiers { get; init; } = new();
    public List<DataHandlingRule> DataHandlingRules { get; init; } = new();
    public List<AccessRole> AccessRoles { get; init; } = new();
    public List<AccessControlRule> AccessControlRules { get; init; } = new();
}

// Handler for the command
public class ProposeGovernanceRulesCommandHandler // Implements your ICommandHandler<,>
{
    // In a real implementation, you would inject your IModelOrchestrator here.
    // private readonly IModelOrchestrator _orchestrator;

    public async Task<ProposedGovernanceFramework> Handle(ProposeGovernanceRulesCommand command)
    {
        // 1. Construct a detailed prompt for the AI.
        //    This prompt asks the AI to act as a governance expert and return a structured JSON.
        var prompt = BuildGovernancePrompt(command.PolicyDocumentText, command.OrgChartText);

        // 2. Call the local LLM via the orchestrator.
        // var jsonResponse = await _orchestrator.InvokePromptAsync(prompt);

        // 3. Deserialize the JSON into the ProposedGovernanceFramework DTO.
        // var proposedFramework = JsonSerializer.Deserialize<ProposedGovernanceFramework>(jsonResponse);

        // For now, returning a mock response.
        return await Task.FromResult(CreateMockFramework());
    }

    private string BuildGovernancePrompt(string policy, string org)
    {
        // Todo: Refine this prompt based on testing and the specific capabilities of your LLM.
        return $"""
        Act as a data governance and security analyst for a organization.
        Analyze the provided Policy Document and Organizational Chart.
        
        Your task is to generate a complete JSON object representing a full governance framework.
        The JSON must contain five top-level keys: "ClassificationTags", "StorageTiers", "DataHandlingRules", "AccessRoles", and "AccessControlRules".
        
        1.  From the Policy Document, identify all distinct data classifications and their handling requirements. Populate "ClassificationTags", "StorageTiers", and "DataHandlingRules".
        2.  From the Organizational Chart, identify all distinct roles. Populate "AccessRoles".
        3.  Based on the principle of least privilege and separation of responsibilities, generate a set of access rules. Populate "AccessControlRules".

        POLICY DOCUMENT:
        ---
        {policy}
        ---

        ORGANIZATIONAL CHART:
        ---
        {org}
        ---

        Respond ONLY with the JSON object.
        """;
    }

    private ProposedGovernanceFramework CreateMockFramework()
    {
        var privilegedTag = new ClassificationTag { Id = Guid.NewGuid(), Name = "LEGAL_PRIVILEGED", Description = "Client-attorney privileged information." };
        var financialTag = new ClassificationTag { Id = Guid.NewGuid(), Name = "FINANCIAL_RECORD", Description = "Invoices, statements, and financial reports." };

        var onPremTier = new StorageTier { Id = Guid.NewGuid(), Name = "On-Premise Encrypted", Location = StorageLocation.OnPremise, EncryptionRequired = true, RetentionPeriodDays = 365 * 10, SeaweedFSCollection = "onprem_secure" };

        var paralegalRole = new AccessRole { Id = Guid.NewGuid(), Name = "Paralegal", Description = "Assists partners with case files." };
        var partnerRole = new AccessRole { Id = Guid.NewGuid(), Name = "Senior Partner", Description = "Manages cases and the firm." };

        return new ProposedGovernanceFramework
        {
            ClassificationTags = new List<ClassificationTag> { privilegedTag, financialTag },
            StorageTiers = new List<StorageTier> { onPremTier },
            DataHandlingRules = new List<DataHandlingRule>
            {
                new() { ClassificationTagId = privilegedTag.Id, StorageTierId = onPremTier.Id },
                new() { ClassificationTagId = financialTag.Id, StorageTierId = onPremTier.Id }
            },
            AccessRoles = new List<AccessRole> { paralegalRole, partnerRole },
            AccessControlRules = new List<AccessControlRule>
            {
                new() { AccessRoleId = paralegalRole.Id, ClassificationTagId = privilegedTag.Id, Permissions = FilePermissions.Read | FilePermissions.Write },
                new() { AccessRoleId = partnerRole.Id, ClassificationTagId = privilegedTag.Id, Permissions = FilePermissions.All },
                new() { AccessRoleId = partnerRole.Id, ClassificationTagId = financialTag.Id, Permissions = FilePermissions.Read }
            }
        };
    }
}
