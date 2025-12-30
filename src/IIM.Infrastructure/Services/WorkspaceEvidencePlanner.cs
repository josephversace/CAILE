using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using IIM.Shared.Dtos;
using IIM.Shared.Interfaces;
using IIM.Shared.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace IIM.Infrastructure.Services
{
	public sealed class WorkspaceEvidencePlanner : IWorkspaceEvidencePlanner
	{
		private readonly IWorkspaceManager _workspaceManager;

        public WorkspaceEvidencePlanner(IWorkspaceManager workspaceManager)
        {
            _workspaceManager = workspaceManager;
        }

		public async Task<WorkspaceEvidencePlan> BuildPlan(
		 WorkspaceIntent intent,
		 IReadOnlyList<object> context,
		 Guid? workspaceId,
		 List<string?> fileHashes,
		 string? modelId = null)
		{
			// Clean up null hashes
			var validHashes = fileHashes.Where(h => !string.IsNullOrEmpty(h)).ToList();

			// ════════════════════════════════════════════════════════════════════
			// SINGLE FILE: Let context manager decide full-text vs chunked
			// ════════════════════════════════════════════════════════════════════
			if (validHashes.Count == 1)
			{
				return intent switch
				{
					WorkspaceIntent.EntityInquiry => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: true,
						IncludeFiles: true,
						IncludeEntities: true,
						IncludeRelationships: true,
						IncludeTimeline: false,
						UseDeterministicSection: false,
						QdrantTopK: 10,
						ModelId: modelId
					),

					WorkspaceIntent.TimelineAnalysis => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: false,
						IncludeFiles: true,
						IncludeEntities: false,
						IncludeRelationships: false,
						IncludeTimeline: true,
						UseDeterministicSection: false,
						QdrantTopK: 12,
						ModelId: modelId
					),

					_ => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: false,
						IncludeFiles: true,
						IncludeEntities: false,
						IncludeRelationships: false,
						IncludeTimeline: false,
						UseDeterministicSection: false,
						QdrantTopK: 10,
						ModelId: modelId
					)
				};
			}

			// ════════════════════════════════════════════════════════════════════
			// MULTIPLE FILES: Balance coverage across files
			// ════════════════════════════════════════════════════════════════════
			if (validHashes.Count > 1)
			{
				return intent switch
				{
					WorkspaceIntent.WorkspaceSummary => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: true,
						IncludeFiles: true,
						IncludeEntities: true,
						IncludeRelationships: true,
						IncludeTimeline: false,
						UseDeterministicSection: false,
						QdrantTopK: Math.Min(validHashes.Count * 3, 15),
						ModelId: modelId
					),

					WorkspaceIntent.RelationshipAnalysis => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: true,
						IncludeFiles: true,
						IncludeEntities: true,
						IncludeRelationships: true,
						IncludeTimeline: false,
						UseDeterministicSection: false,
						QdrantTopK: Math.Min(validHashes.Count * 2, 12),
						ModelId: modelId
					),

					_ => new WorkspaceEvidencePlan(
						UseQdrant: true,
						UseNeo4j: false,
						IncludeFiles: true,
						IncludeEntities: false,
						IncludeRelationships: false,
						IncludeTimeline: false,
						UseDeterministicSection: false,
						QdrantTopK: Math.Min(validHashes.Count * 2, 12),
						ModelId: modelId
					)
				};
			}

			// ════════════════════════════════════════════════════════════════════
			// WORKSPACE-LEVEL (no specific files): Search all files
			// ════════════════════════════════════════════════════════════════════
			return intent switch
			{
				WorkspaceIntent.WorkspaceSummary => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: true,
					IncludeFiles: true,
					IncludeEntities: true,
					IncludeRelationships: true,
					IncludeTimeline: false,
					UseDeterministicSection: false,
					QdrantTopK: 15,
					ModelId: modelId
				),

				WorkspaceIntent.EntityInquiry => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: true,
					IncludeFiles: false,
					IncludeEntities: true,
					IncludeRelationships: true,
					IncludeTimeline: false,
					UseDeterministicSection: false,
					QdrantTopK: 8,
					ModelId: modelId
				),

				WorkspaceIntent.TimelineAnalysis => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: false,
					IncludeFiles: false,
					IncludeEntities: false,
					IncludeRelationships: false,
					IncludeTimeline: true,
					UseDeterministicSection: false,
					QdrantTopK: 10,
					ModelId: modelId
				),

				WorkspaceIntent.FactLookup => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: false,
					IncludeFiles: true,
					IncludeEntities: false,
					IncludeRelationships: false,
					IncludeTimeline: false,
					UseDeterministicSection: false,
					QdrantTopK: 6,
					ModelId: modelId
				),

				WorkspaceIntent.HypothesisTesting => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: true,
					IncludeFiles: true,
					IncludeEntities: true,
					IncludeRelationships: true,
					IncludeTimeline: true,
					UseDeterministicSection: false,
					QdrantTopK: 12,
					ModelId: modelId
				),

				_ => new WorkspaceEvidencePlan(
					UseQdrant: true,
					UseNeo4j: false,
					IncludeFiles: true,
					IncludeEntities: false,
					IncludeRelationships: false,
					IncludeTimeline: false,
					UseDeterministicSection: false,
					QdrantTopK: 8,
					ModelId: modelId
				)
			};
		}
		private static bool AsksForLatest(IReadOnlyList<object> context)
		{
			var userText = context
				.OfType<AGUIMessage>()
				.LastOrDefault(m => m.Role == "user")?.Content;

			if (string.IsNullOrWhiteSpace(userText))
				return false;

			return userText.Contains("latest", StringComparison.OrdinalIgnoreCase)
				|| userText.Contains("most recent", StringComparison.OrdinalIgnoreCase)
				|| userText.Contains("newest", StringComparison.OrdinalIgnoreCase);
		}

	}

}
